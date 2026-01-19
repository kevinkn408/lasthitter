#!/usr/bin/env python3
"""
Context-Aware Project Report Generator
======================================
Drops into any project root, analyzes all files, and generates a comprehensive
context report in markdown format for AI assistants like Cursor or Claude.
"""

import os
import sys
import json
import xml.etree.ElementTree as ET
from pathlib import Path
from datetime import datetime
from typing import Dict, List, Set, Tuple
import mimetypes
import hashlib

# File extensions to process
TEXT_EXTENSIONS = {
    '.py', '.js', '.ts', '.jsx', '.tsx', '.java', '.cpp', '.c', '.h', '.hpp',
    '.cs', '.go', '.rs', '.rb', '.php', '.swift', '.kt', '.scala', '.r',
    '.md', '.txt', '.json', '.xml', '.yaml', '.yml', '.toml', '.ini', '.cfg',
    '.conf', '.sh', '.bash', '.zsh', '.ps1', '.bat', '.sql', '.html', '.css',
    '.scss', '.sass', '.less', '.vue', '.svelte', '.dart', '.lua', '.pl',
    '.pm', '.r', '.m', '.mm', '.h', '.hpp', '.cc', '.cxx', '.proto', '.thrift',
    '.graphql', '.gql', '.dockerfile', '.makefile', '.cmake', '.gradle',
    '.properties', '.env', '.gitignore', '.gitattributes', '.editorconfig'
}

IMAGE_EXTENSIONS = {'.png', '.jpg', '.jpeg', '.gif', '.bmp', '.svg', '.webp', '.ico', '.tiff', '.tif'}

BINARY_EXTENSIONS = {'.exe', '.dll', '.so', '.dylib', '.bin', '.o', '.a', '.lib', '.zip', '.tar', '.gz', '.7z', '.rar'}

# Directories to skip
SKIP_DIRS = {
    '.git', '.svn', '.hg', '__pycache__', 'node_modules', '.venv', 'venv',
    'env', '.env', 'dist', 'build', '.build', 'target', '.idea', '.vscode',
    '.vs', 'bin', 'obj', '.gradle', '.mvn', 'vendor', 'bower_components',
    '.next', '.nuxt', '.cache', 'coverage', '.nyc_output', '.pytest_cache',
    '.mypy_cache', '.ruff_cache', '.DS_Store', 'Thumbs.db'
}

# Files to prioritize (read first)
PRIORITY_FILES = {
    'README.md', 'README.txt', 'readme.md', 'readme.txt',
    'LICENSE', 'LICENSE.txt', 'CHANGELOG.md', 'CHANGELOG.txt',
    'CONTRIBUTING.md', 'package.json', 'requirements.txt', 'Pipfile',
    'pyproject.toml', 'setup.py', 'Cargo.toml', 'pom.xml', 'build.gradle',
    'Makefile', 'Dockerfile', '.gitignore', 'docker-compose.yml',
    'package-lock.json', 'yarn.lock', 'go.mod', 'go.sum'
}


class ContextReportGenerator:
    def __init__(self, root_path: str, output_file: str = "CONTEXT.md", excluded_extensions: Set[str] = None):
        self.root_path = Path(root_path).resolve()
        self.output_file = output_file
        self.excluded_extensions = excluded_extensions or set()
        self.stats = {
            'total_files': 0,
            'text_files': 0,
            'image_files': 0,
            'binary_files': 0,
            'skipped_files': 0,
            'directories': 0
        }
        self.file_contents: Dict[str, str] = {}
        self.file_metadata: Dict[str, Dict] = {}
        self.project_structure: List[str] = []
        self.dependencies: Set[str] = set()
        self.technologies: Set[str] = set()
        
    def should_skip(self, path: Path) -> bool:
        """Check if file/directory should be skipped"""
        # Skip hidden files (except important ones)
        if path.name.startswith('.') and path.name not in {'.gitignore', '.env.example', '.editorconfig'}:
            return True
        
        # Skip directories
        for skip_dir in SKIP_DIRS:
            if skip_dir in path.parts:
                return True
        
        # Skip excluded file extensions
        if path.is_file():
            ext = path.suffix.lower()
            if ext in self.excluded_extensions:
                return True
        
        return False
    
    def detect_language(self, file_path: Path) -> str:
        """Detect programming language from file extension"""
        ext = file_path.suffix.lower()
        lang_map = {
            '.py': 'Python', '.js': 'JavaScript', '.ts': 'TypeScript',
            '.jsx': 'React/JSX', '.tsx': 'React/TSX', '.java': 'Java',
            '.cpp': 'C++', '.c': 'C', '.h': 'C/C++ Header', '.cs': 'C#',
            '.go': 'Go', '.rs': 'Rust', '.rb': 'Ruby', '.php': 'PHP',
            '.swift': 'Swift', '.kt': 'Kotlin', '.scala': 'Scala',
            '.r': 'R', '.m': 'Objective-C', '.mm': 'Objective-C++',
            '.dart': 'Dart', '.lua': 'Lua', '.pl': 'Perl', '.sh': 'Shell',
            '.html': 'HTML', '.css': 'CSS', '.scss': 'SCSS', '.vue': 'Vue',
            '.sql': 'SQL', '.xml': 'XML', '.json': 'JSON', '.yaml': 'YAML',
            '.yml': 'YAML', '.toml': 'TOML', '.md': 'Markdown'
        }
        return lang_map.get(ext, 'Unknown')
    
    def read_text_file(self, file_path: Path, max_size: int = 100000) -> Tuple[str, bool]:
        """Read text file content, truncate if too large"""
        try:
            size = file_path.stat().st_size
            if size > max_size:
                with open(file_path, 'r', encoding='utf-8', errors='ignore') as f:
                    content = f.read(max_size)
                    return content + f"\n\n[File truncated - original size: {size} bytes]", True
            else:
                with open(file_path, 'r', encoding='utf-8', errors='ignore') as f:
                    return f.read(), False
        except Exception as e:
            return f"[Error reading file: {str(e)}]", False
    
    def parse_json(self, content: str) -> Dict:
        """Parse JSON and extract key information"""
        try:
            data = json.loads(content)
            if isinstance(data, dict):
                return {
                    'type': 'JSON Object',
                    'keys': list(data.keys())[:20],  # First 20 keys
                    'sample': str(data)[:500] if len(str(data)) > 500 else str(data)
                }
            return {'type': 'JSON', 'sample': str(data)[:500]}
        except:
            return None
    
    def parse_xml(self, content: str) -> Dict:
        """Parse XML and extract structure"""
        try:
            root = ET.fromstring(content)
            return {
                'type': 'XML',
                'root_tag': root.tag,
                'attributes': dict(root.attrib) if root.attrib else None,
                'children': [child.tag for child in list(root)[:10]]
            }
        except:
            return None
    
    def extract_dependencies(self, file_path: Path, content: str):
        """Extract dependencies from various package files"""
        name = file_path.name.lower()
        
        if name == 'package.json':
            try:
                data = json.loads(content)
                deps = data.get('dependencies', {})
                dev_deps = data.get('devDependencies', {})
                self.dependencies.update(deps.keys())
                self.dependencies.update(dev_deps.keys())
                self.technologies.add('Node.js')
            except:
                pass
        
        elif name in {'requirements.txt', 'requirements-dev.txt'}:
            for line in content.split('\n'):
                line = line.strip().split('#')[0].strip()
                if line and not line.startswith('-'):
                    pkg = line.split('==')[0].split('>=')[0].split('<=')[0].strip()
                    if pkg:
                        self.dependencies.add(pkg)
            self.technologies.add('Python')
        
        elif name == 'pom.xml':
            self.technologies.add('Java/Maven')
        
        elif name in {'build.gradle', 'build.gradle.kts'}:
            self.technologies.add('Java/Gradle')
        
        elif name == 'cargo.toml':
            self.technologies.add('Rust')
        
        elif name == 'go.mod':
            self.technologies.add('Go')
    
    def process_file(self, file_path: Path):
        """Process a single file"""
        if self.should_skip(file_path):
            self.stats['skipped_files'] += 1
            return
        
        self.stats['total_files'] += 1
        rel_path = file_path.relative_to(self.root_path)
        ext = file_path.suffix.lower()
        
        # Detect technology
        lang = self.detect_language(file_path)
        if lang != 'Unknown':
            self.technologies.add(lang)
        
        metadata = {
            'path': str(rel_path),
            'extension': ext,
            'language': lang,
            'size': file_path.stat().st_size,
            'modified': datetime.fromtimestamp(file_path.stat().st_mtime).isoformat()
        }
        
        # Process based on file type
        if ext in TEXT_EXTENSIONS:
            self.stats['text_files'] += 1
            content, truncated = self.read_text_file(file_path)
            metadata['truncated'] = truncated
            
            # Store content for priority files or small files
            if file_path.name in PRIORITY_FILES or file_path.stat().st_size < 50000:
                self.file_contents[str(rel_path)] = content
                self.extract_dependencies(file_path, content)
            
            # Parse structured files
            if ext == '.json':
                parsed = self.parse_json(content)
                if parsed:
                    metadata['parsed'] = parsed
            elif ext == '.xml':
                parsed = self.parse_xml(content)
                if parsed:
                    metadata['parsed'] = parsed
        
        elif ext in IMAGE_EXTENSIONS:
            self.stats['image_files'] += 1
            metadata['type'] = 'Image'
            # Could add image analysis here (dimensions, etc.)
        
        elif ext in BINARY_EXTENSIONS:
            self.stats['binary_files'] += 1
            metadata['type'] = 'Binary'
        
        self.file_metadata[str(rel_path)] = metadata
    
    def build_structure(self, path: Path, prefix: str = "", max_depth: int = 4, current_depth: int = 0):
        """Build directory structure tree"""
        if current_depth >= max_depth:
            return
        
        if self.should_skip(path):
            return
        
        if path.is_dir():
            self.stats['directories'] += 1
            self.project_structure.append(f"{prefix}{path.name}/")
            
            try:
                entries = sorted([e for e in path.iterdir() if not self.should_skip(e)])
                dirs = [e for e in entries if e.is_dir()]
                files = [e for e in entries if e.is_file()]
                
                for i, entry in enumerate(dirs):
                    is_last = (i == len(dirs) - 1) and len(files) == 0
                    new_prefix = prefix + ("└── " if is_last else "├── ")
                    self.build_structure(entry, new_prefix, max_depth, current_depth + 1)
                
                for i, entry in enumerate(files):
                    is_last = i == len(files) - 1
                    marker = "└── " if is_last else "├── "
                    self.project_structure.append(f"{prefix}{marker}{entry.name}")
            except PermissionError:
                pass
    
    def generate_report(self) -> str:
        """Generate the markdown report"""
        report = []
        report.append("# Project Context Report\n")
        report.append(f"**Generated:** {datetime.now().strftime('%Y-%m-%d %H:%M:%S')}\n")
        report.append(f"**Project Root:** `{self.root_path}`\n\n")
        report.append("---\n\n")
        
        # Executive Summary
        report.append("## Executive Summary\n\n")
        report.append(f"- **Total Files:** {self.stats['total_files']}\n")
        report.append(f"- **Text Files:** {self.stats['text_files']}\n")
        report.append(f"- **Image Files:** {self.stats['image_files']}\n")
        report.append(f"- **Binary Files:** {self.stats['binary_files']}\n")
        report.append(f"- **Directories:** {self.stats['directories']}\n")
        report.append(f"- **Skipped Files:** {self.stats['skipped_files']}\n\n")
        
        if self.technologies:
            report.append(f"- **Technologies Detected:** {', '.join(sorted(self.technologies))}\n")
        if self.dependencies:
            report.append(f"- **Dependencies Found:** {len(self.dependencies)} packages\n\n")
        
        report.append("---\n\n")
        
        # Project Structure
        report.append("## Project Structure\n\n")
        report.append("```\n")
        report.append(self.root_path.name + "/\n")
        for line in self.project_structure[:200]:  # Limit structure size
            report.append(line + "\n")
        if len(self.project_structure) > 200:
            report.append(f"... ({len(self.project_structure) - 200} more entries)\n")
        report.append("```\n\n")
        report.append("---\n\n")
        
        # Priority Files Content
        if self.file_contents:
            report.append("## Key Files Content\n\n")
            for file_path, content in sorted(self.file_contents.items()):
                report.append(f"### `{file_path}`\n\n")
                report.append("```\n")
                report.append(content[:5000])  # Limit per file
                if len(content) > 5000:
                    report.append("\n\n[Content truncated for brevity]")
                report.append("\n```\n\n")
        
        # Dependencies
        if self.dependencies:
            report.append("## Dependencies\n\n")
            deps_list = sorted(list(self.dependencies))[:50]  # Top 50
            report.append(", ".join(f"`{dep}`" for dep in deps_list))
            if len(self.dependencies) > 50:
                report.append(f"\n\n*... and {len(self.dependencies) - 50} more*")
            report.append("\n\n---\n\n")
        
        # File Catalog
        report.append("## File Catalog\n\n")
        report.append("### Text Files\n\n")
        text_files = [(p, m) for p, m in self.file_metadata.items() 
                      if m.get('extension') in TEXT_EXTENSIONS]
        text_files.sort()
        
        for file_path, metadata in text_files[:100]:  # Top 100 text files
            lang = metadata.get('language', 'Unknown')
            size_kb = metadata['size'] / 1024
            report.append(f"- `{file_path}` ({lang}, {size_kb:.1f} KB)\n")
        
        if len(text_files) > 100:
            report.append(f"\n*... and {len(text_files) - 100} more text files*\n")
        
        if self.stats['image_files'] > 0:
            report.append("\n### Image Files\n\n")
            image_files = [(p, m) for p, m in self.file_metadata.items() 
                          if m.get('extension') in IMAGE_EXTENSIONS]
            image_files.sort()
            for file_path, metadata in image_files[:50]:
                size_kb = metadata['size'] / 1024
                report.append(f"- `{file_path}` ({size_kb:.1f} KB)\n")
        
        report.append("\n---\n\n")
        report.append("## Notes\n\n")
        report.append("- This report was generated automatically by `context_report.py`\n")
        report.append("- Large files may be truncated\n")
        report.append("- Binary files and common build/cache directories are excluded\n")
        report.append("- Use this report to quickly understand project structure and context\n")
        
        return "".join(report)
    
    def count_total_files(self) -> int:
        """Count total files that will be processed"""
        count = 0
        for root, dirs, files in os.walk(self.root_path):
            root_path = Path(root)
            # Filter directories
            dirs[:] = [d for d in dirs if not self.should_skip(root_path / d)]
            for file in files:
                file_path = root_path / file
                if not self.should_skip(file_path):
                    count += 1
        return count
    
    def print_progress(self, current: int, total: int, filename: str = ""):
        """Print progress with percentage"""
        if total == 0:
            percent = 100
        else:
            percent = min(100, int((current / total) * 100))
        
        # Create progress bar
        bar_length = 40
        filled = int(bar_length * current / total) if total > 0 else bar_length
        bar = '█' * filled + '░' * (bar_length - filled)
        
        # Truncate filename if too long
        display_name = filename
        if len(display_name) > 50:
            display_name = "..." + display_name[-47:]
        
        # Print on same line (overwrite)
        print(f"\r📄 [{bar}] {percent}% ({current}/{total}) {display_name}", end='', flush=True)
    
    def run(self):
        """Main execution method"""
        print(f"🔍 Analyzing project: {self.root_path}")
        print("📁 Building directory structure...")
        self.build_structure(self.root_path)
        
        print("🔢 Counting files...")
        total_files = self.count_total_files()
        print(f"📊 Found {total_files} files to process\n")
        
        print("📄 Processing files...")
        processed = 0
        
        for root, dirs, files in os.walk(self.root_path):
            root_path = Path(root)
            
            # Filter directories
            dirs[:] = [d for d in dirs if not self.should_skip(root_path / d)]
            
            for file in files:
                file_path = root_path / file
                try:
                    self.process_file(file_path)
                    processed += 1
                    rel_path = file_path.relative_to(self.root_path)
                    self.print_progress(processed, total_files, str(rel_path))
                except Exception as e:
                    processed += 1
                    self.print_progress(processed, total_files, f"Error: {file}")
                    # Don't print error here to avoid cluttering progress
        
        print("\n📝 Generating report...")
        report = self.generate_report()
        
        output_path = self.root_path / self.output_file
        with open(output_path, 'w', encoding='utf-8') as f:
            f.write(report)
        
        print(f"✅ Report generated: {output_path}")
        print(f"📊 Statistics: {self.stats}")
        return output_path


def main():
    """Entry point - analyzes the directory where this script is located
    
    Usage:
        context_report.py [output_file] [-.ext1] [-.ext2] ...
    
    Examples:
        context_report.py                          # Default: CONTEXT.md, all files
        context_report.py PROJECT.md                # Custom output file
        context_report.py -.cs -.tga                # Exclude .cs and .tga files
        context_report.py CONTEXT.md -.cs -.meta   # Custom output + exclusions
    """
    # Get the directory where this script lives
    script_dir = Path(__file__).parent.resolve()
    
    # Parse command line arguments
    output_file = "CONTEXT.md"
    excluded_extensions = set()
    
    for arg in sys.argv[1:]:
        if arg.startswith('-.'):
            # Exclusion flag: -.ext
            ext = arg[1:].lower()  # Remove the '-' and get extension
            if not ext.startswith('.'):
                ext = '.' + ext  # Ensure it starts with .
            excluded_extensions.add(ext)
        elif not arg.startswith('-'):
            # First non-flag argument is the output filename
            if output_file == "CONTEXT.md":
                output_file = arg
    
    # Print configuration
    print(f"📁 Script location: {script_dir}")
    print(f"📄 Output file: {output_file}")
    if excluded_extensions:
        print(f"🚫 Excluded extensions: {', '.join(sorted(excluded_extensions))}")
    print()
    
    generator = ContextReportGenerator(str(script_dir), output_file, excluded_extensions)
    generator.run()


if __name__ == "__main__":
    main()
