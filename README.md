# 🔍 BBRepoList
BBRepoList is a lightweight utility for fetching the list of repositories available in your Bitbucket workspace (namespace), with support for filtering repositories by name.

## Bitbucket settings (appsettings.json)
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

## Output
The utility outputs a list of repositories returned by the Bitbucket workspace query. Depending on configuration, the output can be:
- Printed to the console for quick inspection.
- Written to a file for reuse in scripts or reports.

Example output:
![Example output](BBList.png)
