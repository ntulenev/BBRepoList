# 🔍 BBRepoList
<img src="BBRepoList.png" alt="BBRepoList logo" width="250">

## 📘 About
BBRepoList is a lightweight utility for fetching the list of repositories available in your Bitbucket workspace (namespace), with support for filtering repositories by name.

## 🌐 Bitbucket REST API
This app is built on the Bitbucket REST API contract documented here:
```
https://developer.atlassian.com/cloud/bitbucket/rest/
```

## ⚙️ Bitbucket settings (appsettings.json)
Example:
```json
{
  "Bitbucket": {
    "BaseUrl": "",
    "Workspace": "",
    "AuthEmail": "",
    "AuthApiToken": "",
    "PageLen": 50
  }
}
```

- `BaseUrl`: Base API endpoint.
- `Workspace`: Your Bitbucket workspace (namespace) to list repositories from.
- `AuthEmail`: The Bitbucket account email used for authentication.
- `AuthApiToken`: Your Bitbucket API token.
- `PageLen`: Number of repositories to request per page (pagination size).

## 📄 Output
The utility outputs a list of repositories returned by the Bitbucket workspace query.

![Example output](BBList.png)
