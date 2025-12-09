# Tribes Font Color Tags Reference

## Available Font Tags for centerprint/bottomprint

The font tags `<f0>` through `<f8>` control text colors in `centerprint` and `bottomprint` messages.

### Standard Font Tags

| Tag | Color | Common Usage | Example in Codebase |
|-----|-------|--------------|-------------------|
| `<f0>` | **White/Default** | Default text color, used to reset after colored text | Used after `<f1>` or `<f2>` to return to default |
| `<f1>` | **Red** | Important messages, warnings, player names, headers | `<f1>PlayerName`, `<f1>PK Rules:`, vote messages |
| `<f2>` | **Green** | Labels, positive info, house names | `<f2>DEF:`, `<f2>HP:`, `<f2>Weight:`, house display |
| `<f3>` | **Yellow** | (Rarely used) | Not commonly seen in codebase |
| `<f4>` | **Blue** | Information messages, suggestions | `<f4>If you have suggestions...` |
| `<f5>` | **Cyan/Light Blue** | Rules, host info | `<f5>PK Rules:`, `<f5>Your Host: Jobo` |
| `<f6>` | **Pink/Magenta** | (Rarely used) | Not commonly seen in codebase |
| `<f7>` | **White** | (Alternative white) | Not commonly seen in codebase |
| `<f8>` | **Yellow/Bright Yellow** | Welcome messages, important announcements | `<f8>Welcome To The Kingdom of Kronos RPG!` |

### Usage Patterns in Your Codebase

**From `rpgfunk.cs` and `Admin.cs`:**
- `<f0>` - Default white text (most common for body text)
- `<f1>` - Red for headers, player names, important info
- `<f2>` - Green for labels (DEF:, HP:, Weight:, etc.)
- `<f4>` - Blue for informational messages
- `<f5>` - Cyan for rules and host information
- `<f8>` - Yellow for welcome/announcement messages

### Example Usage

```cs
// Header with red name, default text
"<f1>PlayerName<f0>, LEVEL 50"

// Labels in green, values in default
"<f2>DEF:<f0> 100  <f2>MDEF:<f0> 50  <f2>ATK:<f0> 150"

// Welcome message in yellow
"<f8>Welcome To The Kingdom of Kronos RPG!"

// Rules in cyan
"<f5>PK Rules:"
```

### Notes

- Font tags affect all text **after** the tag until another font tag is encountered
- Always use `<f0>` to reset to default color after using colored text
- Font tags work in both `centerprint` and `bottomprint`
- Font tags do **NOT** work in chat messages (chat uses caret codes `^0`-`^9` instead)

### Color Scheme Recommendations

For the stats menu, consider:
- **Headers**: `<f1>` (Red) - for player name, level, class
- **Labels**: `<f2>` (Green) - for stat names like "ATK:", "DEF:", "HP:"
- **Values**: `<f0>` (White) - for actual numbers
- **Important info**: `<f8>` (Yellow) - for warnings or highlights

