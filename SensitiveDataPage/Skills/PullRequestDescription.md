---
name: PullRequestDescription
description: As a Developer, create description for Pull Request based ONLY on committed changes from current branch compared to current master. 
When user finishes work, I review code changes and generate PR description.
I compare current branch only with current master using git diff command.
---

# PullRequestDescription

## Overwiev
Generate professional analysis and description of the pull request, including code changes, potential issues, and suggestions for improvement.

## Step 0: Staged changes handling
1. If there are staged changes:
   - I create a single commit for them
   - I push commit to current branch
2. If there are no staged changes:
   - I do nothing
3. I do NOT stage any new changes
4. I do NOT modify any files manually

## Step 1: Review the Pull Request

1. Analyze ONLY committed changes (git diff current branch vs master)
2. I do NOT go through whole project, only diff
3. Keep code quality, readability, and maintainability in mind
4. Make sure code is strict to CUPID guidelines and best practices
5. Make sure new code is consistent with existing code
6. Make sure proper unit, component, integration and e2e tests are present
7. I do NOT include unstaged or uncommitted changes

## Step 2: Provide Feedback

1. Write clear and concise pull request description (TEXT ONLY)
2. If pull request description alreay exist, modife it
3. I do NOT create any files
4. I do NOT push any description to repository
5. I provide constructive feedback with issues and improvements

## Step 3: Quality check

1. No placeholder like "[your description here]"
2. I do NOT create any new files inside project
3. I do NOT execute any scripts, tools or external files (including skill.md)
4. Only allowed repository actions: commit and push (from Step 0)

## Step 4: Description check

1. I use:
   - Title
   - Summary
   - Key changes
   - Review checklist
   - Description foundations
2. I don't use any other sections

## Final Output

Return ONLY PR description as plain text.
No additional comments.
No explanations.