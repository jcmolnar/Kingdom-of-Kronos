# Kingdom of Kronos Git Workflow

This guide outlines the standard procedure for working with the **Kingdom of Kronos** repository.

## 1. Start of Session: Get Latest Changes
Before you start working, always make sure you have the latest code.
```powershell
git pull
```

## 2. During Development: Check Status
At any point, you can see what files you have modified.
```powershell
git status
```
> [!NOTE]
> The `Temp/` directory is automatically ignored. You will not see files from that folder in the status list.

## 3. Saving Your Work (Commit)
When you have made progress and want to save a snapshot of your work:

### Step A: Stage Changes
Select the files you want to include in this snapshot.
```powershell
# Add all changed files
git add .
```

### Step B: Commit Changes
Save the staged files with a descriptive message.
```powershell
git commit -m "Describe what you changed here"
```

## 4. Syncing with GitHub (Push)
When you are ready to upload your commits to GitHub:
```powershell
git push
```

## Summary of Commands
| Action | Command |
| :--- | :--- |
| **Get Updates** | `git pull` |
| **View Changes** | `git status` |
| **Stage All** | `git add .` |
| **Commit** | `git commit -m "message"` |
| **Upload** | `git push` |

> [!IMPORTANT]
> Always check `git status` before adding files to ensure you aren't committing anything unexpected.

## Best Practices: Avoiding "Mega-Commits"
To prevent losing work or getting confused, try to commit:
- **Every time you finish a specific feature or bugfix.**
- **Before starting a different task.**
- **If you've been working for more than an hour.**
- **Before you leave your computer.**

*Tip: If I (the AI) am helping you, I will try to check your status and remind you if I see a lot of uncommitted changes!*

