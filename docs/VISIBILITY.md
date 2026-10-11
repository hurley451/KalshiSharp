# KalshiSharp Visibility Tracking

This page records the repository's discovery baseline and the repeatable checks used to track
visibility. Update it monthly or after major releases.

## Baseline Snapshot

Captured on 2026-10-10 from GitHub and NuGet public APIs.

| Metric | Value |
|--------|-------|
| GitHub stars | 6 |
| Forks | 2 |
| Watchers/subscribers | 1 |
| Contributors | 2 |
| NuGet total downloads | 626 |
| External GitHub PackageReference hits | 0 |
| External GitHub issue/PR mentions | 0 |
| Rolling 14-day GitHub views | 53 total, 5 unique |
| Rolling 14-day GitHub clones | 283 total, 138 unique |

## Monthly Checks

Run these checks from a repository checkout with `gh` authenticated.

```bash
gh repo view --json nameWithOwner,description,homepageUrl,repositoryTopics,stargazerCount,forkCount,watchers
gh api repos/{owner}/{repo}/traffic/views
gh api repos/{owner}/{repo}/traffic/clones
gh api repos/{owner}/{repo}/traffic/popular/referrers
gh api repos/{owner}/{repo}/traffic/popular/paths
gh api 'search/code?q=%22PackageReference%22+%22KalshiSharp%22+-repo:hurley451/KalshiSharp&type=code'
gh api 'search/issues?q=%22KalshiSharp%22+-repo:hurley451/KalshiSharp&type=issues'
curl -fsSL 'https://azuresearch-usnc.nuget.org/query?q=packageid:KalshiSharp&prerelease=false'
```

## Visibility Objectives

- Improve GitHub search matching through topics, README headings, and package metadata.
- Convert NuGet discovery into GitHub engagement by linking examples and API coverage clearly.
- Seed organic references through small examples and answerable issues/discussions.
- Track external usage separately from automated catalog mirrors.

## Organic Reference Ideas

- Publish small example apps that reference the package directly.
- Link KalshiSharp in relevant C#/.NET prediction-market demos.
- Use GitHub Discussions for support and API-change questions.
- Keep issue titles specific to API surfaces so searchers can find answers.
