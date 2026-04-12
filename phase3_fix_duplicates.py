#!/usr/bin/env python3
"""Fix duplicate Color= and Variant= attrs on MudButton tags, and revert bad Class= merges."""

import re
from pathlib import Path

CLIENT_DIR = Path("/home/runner/work/EventForge/EventForge/EventForge.Client")


def fix_duplicate_attrs_in_tag(tag_text: str, attr_name: str) -> str:
    """Remove all but the first occurrence of attr_name= in a tag."""
    pattern = re.compile(r'\s+' + re.escape(attr_name) + r'\s*=\s*"[^"]*"', re.IGNORECASE)
    matches = list(pattern.finditer(tag_text))
    if len(matches) <= 1:
        return tag_text
    # Remove all but the first match (process in reverse order)
    result = tag_text
    for m in reversed(matches[1:]):
        result = result[:m.start()] + result[m.end():]
    return result


def find_mud_button_tags(content: str):
    results = []
    i = 0
    n = len(content)
    pattern = '<MudButton'
    while i < n:
        idx = content.find(pattern, i)
        if idx == -1:
            break
        after = idx + len(pattern)
        if after < n and content[after] not in ' \t\r\n/>':
            i = idx + 1
            continue
        j = after
        in_string = None
        depth_paren = 0
        while j < n:
            c = content[j]
            if in_string:
                if c == in_string and depth_paren == 0:
                    in_string = None
                elif c == '@' and in_string == '"' and j + 1 < n and content[j + 1] == '(':
                    depth_paren += 1
                    j += 1
                elif c == '(' and depth_paren > 0:
                    depth_paren += 1
                elif c == ')' and depth_paren > 0:
                    depth_paren -= 1
            else:
                if c == '"':
                    in_string = '"'
                elif c == "'":
                    in_string = "'"
                elif c == '>':
                    j += 1
                    results.append((idx, j))
                    break
            j += 1
        i = idx + 1
    return results


def process_file(filepath: Path) -> int:
    content = filepath.read_text(encoding='utf-8')
    original = content
    tags = find_mud_button_tags(content)
    changes = 0
    for start, end in reversed(tags):
        tag = content[start:end]
        new_tag = fix_duplicate_attrs_in_tag(tag, 'Color')
        new_tag = fix_duplicate_attrs_in_tag(new_tag, 'Variant')
        if new_tag != tag:
            content = content[:start] + new_tag + content[end:]
            changes += 1
    if content != original:
        filepath.write_text(content, encoding='utf-8')
    return changes


def main():
    total = 0
    for fpath in sorted(CLIENT_DIR.rglob("*.razor")):
        n = process_file(fpath)
        if n > 0:
            print(f"  {fpath.relative_to(CLIENT_DIR)}: {n} duplicate attr(s) removed")
            total += n
    print(f"Total: {total} tags fixed")


if __name__ == '__main__':
    main()
