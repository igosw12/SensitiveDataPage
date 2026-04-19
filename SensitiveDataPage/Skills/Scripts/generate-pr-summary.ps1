$BaseBranch = "master"
$DiffFile = "diff.txt"
$CommitsFile = "commits.txt"
$OutputFile = "pr_summary.md"

Write-Host "Generating diff..."
git diff $BaseBranch...HEAD | Out-File $DiffFile -Encoding utf8

Write-Host "Generating commits..."
git log $BaseBranch..HEAD --oneline | Out-File $CommitsFile -Encoding utf8

Write-Host "➡️ Loading..."

$Skill = Get-Content ..\ProjectManager.md -Raw
$Diff = Get-Content $DiffFile -Raw
$Commits = Get-Content $CommitsFile -Raw

$ApiKey = $env:AIStrings__OpenAIKey

$Prompt = @"
$Skill

## GIT DIFF
$Diff

## COMMITS
$Commits
"@

Write-Host "➡️ Sending to OpenAI..."

$Body = @{
    model = "gpt-5.4-mini"
    input = $Prompt
} | ConvertTo-Json -Depth 10

$Headers = @{
    "Authorization" = "Bearer $ApiKey"
    "Content-Type"  = "application/json"
}

$Response = Invoke-RestMethod -Uri "https://api.openai.com/v1/responses" -Method Post -Headers $Headers -Body $Body

$Response.output[0].content[0].text | Out-File $OutputFile -Encoding utf8

Write-Host "✅ Ready: $OutputFile"