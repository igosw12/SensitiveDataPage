---
name: PullRequestDescription
description: As a Developer, create description for Pull Request based ONLY on committed changes from current branch compared to current master. 
When user finishes work, I review code changes and generate PR description.
I compare current branch only with current master using git diff command.
---

# PullRequestDescription

## Overwiev
Generate professional analysis and description of the pull request, including code changes, potential issues, and suggestions for improvement.

## Whitelisted commands:
1. git status
2. git diff
3. git log
4. gh pr edit
5. gh pr create
6. gh pr view
7. git commit
8. git push

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
3. I update pull request description on GitHub.com for current PR (gh pr edit --body-file)
4. I do NOT create or modify source code files in repository for description publishing
5. I provide constructive feedback with issues and improvements

## Step 3: Quality check

1. No placeholder like "[your description here]"
2. I do NOT create any new files inside project
3. I do NOT execute external files except required GitHub CLI command for PR description update
4. Allowed actions: commit/push from Step 0 and PR description update on GitHub.com

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