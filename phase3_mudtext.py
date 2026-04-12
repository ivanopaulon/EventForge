#!/usr/bin/env python3
"""
Phase 3 - Task 3: Replace MudText Style= with CSS utility classes.
Uses a state-machine Razor tag parser that handles quotes and @() expressions.
"""

import os
import re
import sys
from pathlib import Path

CLIENT_DIR = Path("/home/runner/work/EventForge/EventForge/EventForge.Client")

# ---------------------------------------------------------------------------
# Style → Class mapping table
# Keys are normalised style strings (lowercase, no spaces around : and ;,
# no trailing semicolon).  Values are the CSS class string to use.
# ---------------------------------------------------------------------------

def normalise_style(style: str) -> str:
    """Normalise a style string for matching: lowercase, collapse whitespace around : and ;."""
    s = style.strip()
    # Collapse all whitespace
    s = re.sub(r'\s+', ' ', s)
    # Normalise around : and ;
    s = re.sub(r'\s*:\s*', ':', s)
    s = re.sub(r'\s*;\s*', ';', s)
    # Remove trailing semicolon
    s = s.rstrip(';')
    return s.lower()

STYLE_MAP = {
    # white-space
    "white-space:nowrap": "text-nowrap",
    "white-space:pre-wrap": "text-prewrap",
    "white-space:pre-wrap;word-break:break-word": "text-prewrap text-breakword",
    "word-break:break-word;white-space:pre-wrap": "text-prewrap text-breakword",
    # word-break
    "word-break:break-all": "text-breakall",
    # overflow / ellipsis
    "overflow:hidden;text-overflow:ellipsis;white-space:nowrap": "text-truncate-ef",
    "text-overflow:ellipsis;overflow:hidden;white-space:nowrap": "text-truncate-ef",
    "white-space:nowrap;overflow:hidden;text-overflow:ellipsis": "text-truncate-ef",
    # opacity
    "opacity:.7": "text-opacity-70",
    "opacity:0.7": "text-opacity-70",
    "opacity:0.8": "text-opacity-80",
    # display
    "display:inline": "d-inline",
    # flex
    "flex:1": "flex-1",
    "flex-shrink:0": "flex-shrink-0",
    # max-width
    "max-width:200px": "ef-max-200",
    "max-width:300px": "ef-max-300",
    "max-width:400px": "ef-max-400",
    # colours
    "color:rgba(255,255,255,0.5);text-transform:uppercase;letter-spacing:0.08em": "text-section-label",
    "color:rgba(255,255,255,.5);text-transform:uppercase;letter-spacing:0.08em": "text-section-label",
    "color:rgba(255,255,255,0.55)": "text-dark-muted",
    "color:rgba(255,255,255,.55)": "text-dark-muted",
    "color:rgba(255,255,255,0.7)": "text-dark-secondary",
    "color:rgba(255,255,255,.7)": "text-dark-secondary",
    "color:rgba(255,255,255,0.70)": "text-dark-secondary",
    "color:rgba(255,255,255,0.70);margin-top:4px": "text-dark-secondary mt-1-px",
    "color:rgba(255,255,255,0.87)": "text-dark-primary",
    "color:rgba(255,255,255,.87)": "text-dark-primary",
    "color:rgba(255,255,255,1)": "text-dark-full",
    "color:white": "text-dark-full",
}


# ---------------------------------------------------------------------------
# Razor-aware attribute tokeniser
# ---------------------------------------------------------------------------

def parse_tag_attributes(tag_content: str):
    """
    Given the inner content of an opening tag (everything between < and >),
    return a list of (name, value, raw_text) tuples plus raw whitespace segments.
    
    Actually we just need to find and replace individual attributes while
    preserving surrounding text – we'll do this differently: parse the whole
    tag into a list of tokens where each token is either whitespace/other text
    or an attribute assignment.
    
    Returns list of (kind, text) where kind is 'attr' or 'other'.
    For 'attr': text is the full attribute text e.g. 'Style="foo"'
    """
    tokens = []
    i = 0
    n = len(tag_content)
    
    while i < n:
        # Skip whitespace
        ws_start = i
        while i < n and tag_content[i] in ' \t\r\n':
            i += 1
        if i > ws_start:
            tokens.append(('other', tag_content[ws_start:i]))
        
        if i >= n:
            break
        
        # Check for attribute name
        # Attribute names: letters, digits, -, _, @, :
        if tag_content[i] not in '<>/':
            attr_start = i
            # Read attribute name
            while i < n and tag_content[i] not in '= \t\r\n/>':
                i += 1
            attr_name = tag_content[attr_start:i]
            
            # Skip whitespace around =
            while i < n and tag_content[i] in ' \t\r\n':
                i += 1
            
            if i < n and tag_content[i] == '=':
                i += 1  # consume =
                # Skip whitespace
                while i < n and tag_content[i] in ' \t\r\n':
                    i += 1
                
                if i < n:
                    attr_val_start = i
                    if tag_content[i] == '"':
                        # Standard quoted attribute - but may contain @() expressions
                        i += 1
                        depth_paren = 0
                        while i < n:
                            c = tag_content[i]
                            if c == '@' and i+1 < n and tag_content[i+1] == '(':
                                depth_paren += 1
                                i += 2
                            elif c == '(' and depth_paren > 0:
                                depth_paren += 1
                                i += 1
                            elif c == ')' and depth_paren > 0:
                                depth_paren -= 1
                                i += 1
                            elif c == '"' and depth_paren == 0:
                                i += 1
                                break
                            else:
                                i += 1
                        attr_val = tag_content[attr_val_start:i]
                        tokens.append(('attr', attr_name + '=' + attr_val))
                    elif tag_content[i] == "'":
                        i += 1
                        while i < n and tag_content[i] != "'":
                            i += 1
                        if i < n:
                            i += 1
                        attr_val = tag_content[attr_val_start:i]
                        tokens.append(('attr', attr_name + '=' + attr_val))
                    elif tag_content[i] == '@':
                        # @expression or @variable
                        i += 1
                        if i < n and tag_content[i] == '(':
                            # @(...) 
                            depth = 1
                            i += 1
                            while i < n and depth > 0:
                                if tag_content[i] == '(':
                                    depth += 1
                                elif tag_content[i] == ')':
                                    depth -= 1
                                i += 1
                        else:
                            # @variable - read until whitespace or />
                            while i < n and tag_content[i] not in ' \t\r\n/>':
                                i += 1
                        attr_val = tag_content[attr_val_start:i]
                        tokens.append(('attr', attr_name + '=' + attr_val))
                    else:
                        # Unquoted value
                        while i < n and tag_content[i] not in ' \t\r\n/>':
                            i += 1
                        attr_val = tag_content[attr_val_start:i]
                        tokens.append(('attr', attr_name + '=' + attr_val))
                else:
                    tokens.append(('attr', attr_name + '='))
            else:
                # Boolean attribute (no value)
                tokens.append(('attr', attr_name))
        else:
            tokens.append(('other', tag_content[i]))
            i += 1
    
    return tokens


def get_attr_name_value(attr_text: str):
    """Given 'Name="value"', return ('Name', 'value') or ('Name', None) for boolean."""
    eq_idx = attr_text.find('=')
    if eq_idx == -1:
        return attr_text, None
    name = attr_text[:eq_idx]
    val_part = attr_text[eq_idx+1:]
    if val_part and val_part[0] in '"\'':
        # Strip outer quotes
        return name, val_part[1:-1]
    return name, val_part


def process_mud_tag(tag_text: str, component_name: str) -> str:
    """
    Process a single MudText/MudIcon opening tag, applying Style→Class substitutions.
    Returns the modified tag text.
    """
    # Split off the tag name prefix  e.g. "<MudText " or "<MudText"
    # Find where attributes start
    m = re.match(r'^(<\s*' + re.escape(component_name) + r')', tag_text)
    if not m:
        return tag_text
    
    prefix = m.group(1)
    rest = tag_text[len(prefix):]
    # rest ends with > or />
    if rest.endswith('/>'):
        suffix = '/>'
        rest = rest[:-2]
    elif rest.endswith('>'):
        suffix = '>'
        rest = rest[:-1]
    else:
        return tag_text  # malformed
    
    tokens = parse_tag_attributes(rest)
    
    # Find Style= and Class= attributes
    style_idx = None
    class_idx = None
    for i, (kind, text) in enumerate(tokens):
        if kind == 'attr':
            name, _ = get_attr_name_value(text)
            name_lower = name.lower()
            if name_lower == 'style':
                style_idx = i
            elif name_lower == 'class':
                class_idx = i
    
    if style_idx is None:
        return tag_text  # nothing to do
    
    _, style_val = get_attr_name_value(tokens[style_idx][1])
    if style_val is None:
        return tag_text
    
    norm = normalise_style(style_val)
    new_class = STYLE_MAP.get(norm)
    
    if new_class is None:
        return tag_text  # no mapping
    
    # Build new tokens
    new_tokens = list(tokens)
    
    if class_idx is not None:
        # Merge into existing Class=
        _, existing_class = get_attr_name_value(tokens[class_idx][1])
        merged = (existing_class or '').strip() + ' ' + new_class
        merged = merged.strip()
        class_name = tokens[class_idx][1].split('=')[0]
        new_tokens[class_idx] = ('attr', f'{class_name}="{merged}"')
        # Remove Style= token and adjacent whitespace
        # Remove Style token
        new_tokens[style_idx] = ('other', '')
    else:
        # Replace Style= with Class=
        style_attr_name = tokens[style_idx][1].split('=')[0]
        # Replace Style with Class, keeping same name case as original...
        # Actually just use Class=
        new_tokens[style_idx] = ('attr', f'Class="{new_class}"')
    
    result = prefix + ''.join(t[1] for t in new_tokens) + suffix
    return result


def find_mud_tags(content: str, component_name: str):
    """
    Find all opening tags for component_name in content.
    Returns list of (start, end) positions for each tag.
    This is a state machine that handles nested quotes and @() expressions.
    """
    results = []
    i = 0
    n = len(content)
    pattern = f'<{component_name}'
    
    while i < n:
        # Find next potential tag start
        idx = content.find(pattern, i)
        if idx == -1:
            break
        
        # Verify it's followed by whitespace or / or >
        after = idx + len(pattern)
        if after < n and content[after] not in ' \t\r\n/>':
            i = idx + 1
            continue
        
        # Now scan to end of tag
        j = after
        in_string = None
        depth_paren = 0
        
        while j < n:
            c = content[j]
            
            if in_string:
                if c == in_string and depth_paren == 0:
                    in_string = None
                elif c == '@' and in_string == '"' and j+1 < n and content[j+1] == '(':
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


def process_file(filepath: Path, component_name: str) -> int:
    """Process a single .razor file. Returns number of replacements."""
    content = filepath.read_text(encoding='utf-8')
    original = content
    
    changes = 0
    # Process tags from right to left to preserve positions
    tags = find_mud_tags(content, component_name)
    
    for start, end in reversed(tags):
        tag_text = content[start:end]
        new_tag = process_mud_tag(tag_text, component_name)
        if new_tag != tag_text:
            content = content[:start] + new_tag + content[end:]
            changes += 1
    
    if content != original:
        filepath.write_text(content, encoding='utf-8')
    
    return changes


def main():
    total = 0
    razor_files = list(CLIENT_DIR.rglob("*.razor"))
    
    for component in ["MudText"]:
        comp_changes = 0
        for fpath in razor_files:
            n = process_file(fpath, component)
            if n > 0:
                print(f"  {fpath.relative_to(CLIENT_DIR)}: {n} {component} style(s) replaced")
                comp_changes += n
        print(f"{component}: {comp_changes} replacements")
        total += comp_changes
    
    print(f"\nTotal: {total} replacements")


if __name__ == '__main__':
    main()
