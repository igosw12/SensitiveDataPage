$BaseBranch = "master"
$DiffFile = "diff.txt"
$CommitsFile = "commits.txt"
$OutputFile = "pr_summary.md"
$PublishToGitHub = $true

Write-Host "Generating diff..."
git diff $BaseBranch...HEAD | Out-File $DiffFile -Encoding utf8

Write-Host "Generating commits..."
git log $BaseBranch..HEAD --oneline | Out-File $CommitsFile -Encoding utf8

Write-Host "Loading context..."

$Skill = Get-Content ..\ProjectManager.md -Raw
$Diff = Get-Content $DiffFile -Raw
$Commits = Get-Content $CommitsFile -Raw

$ApiKey = $env:AIStrings__OpenAIKey

if ([string]::IsNullOrWhiteSpace($ApiKey)) {
    throw "Missing API key. Set AIStrings__OpenAIKey environment variable."
}

$Prompt = @"
$Skill

## GIT DIFF
$Diff

## COMMITS
$Commits
"@

Write-Host "Sending request to OpenAI..."

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

Write-Host "Ready: $OutputFile"

if (-not $PublishToGitHub) {
    Write-Host "GitHub update skipped (PublishToGitHub=false)."
    exit 0
}

$GhCommand = Get-Command gh -ErrorAction SilentlyContinue
if (-not $GhCommand) {
    Write-Warning "GitHub CLI (gh) not found. Install gh to publish PR description automatically."
    exit 0
}

try {
    $PrNumber = gh pr view --json number --jq ".number"
    if ([string]::IsNullOrWhiteSpace($PrNumber)) {
        Write-Warning "No active pull request found for this branch."
        exit 0
    }

    gh pr edit $PrNumber --body-file $OutputFile | Out-Null
    $PrUrl = gh pr view $PrNumber --json url --jq ".url"
    Write-Host "PR description updated on GitHub.com: $PrUrl"
}
catch {
    Write-Warning "Failed to update PR description on GitHub.com. $_"
}