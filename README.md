# ApiParser
Provides functionality to resolve queries against the GW2 API via the [Gw2Sharp](https://github.com/Archomeda/Gw2Sharp) dependency.

Queries can either be explicitly created via the `EndpointQuery` class, or parsed from a string
 via `EndpointQuery.FromString(string, ParseSettings?)`. Queries may contain variables, that can
 be resolved if an `IQueryVariableResolver` is provided.

All request should be made via the `ApiManager`, which also keeps a cache of the retrieved data to reduce the API requests.

The `ApiManager` and `EndpointQuery`ies are customizable if custom `ApiManagerSettings`, `ParseSettings` or `QuerySettings` are
 provided.

> [!CAUTION]
> This is a first draft and I mostly plan to use this library for myself. I might change implementation details and 
> introduce breaking changes without much regard for other users.
> 
> That said, I'll still try to avoid breaking changes and separate them by new releases if they are introduced.

## Limitations
Currently only endpoints that are either all expandable (all the data of the endpoint can be retrieved with one API call)
 or that carry blob data are supported. Endpoints that require pagination are therefor not supported.

Querying data for multiple ids at the same time is not supported. You have to either query the data for a single id, or the
 whole endpoint and then select the data from there.

A query will return an `object`. If you need a more specific type, you have to cast the query result.

> [!NOTE]
> I have currently no intention of addressing any of those limitations, since I currently don't need the functionality.
> This might change in the future, or it might not.

## Exceptions
This library might return a multitude of exceptions. All custom exceptions inherit from `ApiParserException`.

If the API responds with an error, the `RequestException` thrown by [Gw2Sharp](https://github.com/Archomeda/Gw2Sharp) will
 be wrapped inside a `EndpointRequestException`.

At some point I will add more documentation for exceptions. If you need information on exceptions right now, please take a look
 at their [implementation](_Exceptions).

## Examples
All examples use the default `ParseSettings`.

### Retrieve the whole data from the Account endpoint
```csharp
// construct the query
var query = EndpointQuery.FromString($"Account");

// instantiate the apiManager
// assumes client to be a Gw2Sharp.Gw2Client
// will not refresh the cached data, if a request is made for 2 minutes since the last refresh.
var apiManager = new ApiManager(client.WebApi.V2, new ApiParser.V2.Settings.ApiManagerSettings() { Cooldown = 120_000 });

// might throw exceptions
var queryResult = await apiManager.ResolveQuery(query);

// check if the result contains the expected data
if (queryResult is Gw2Sharp.WebApi.V2.Models.Account accountData)
{
	// do stuff
}
```
**Result**
The result is a `Gw2Sharp.WebApi.V2.Models.Account` object. For reference see
 the [Gw2Sharp implementation](https://github.com/Archomeda/Gw2Sharp/blob/master/Gw2Sharp/WebApi/V2/Models/Account/Account.cs).

The underlying API response might look a little bit like this[^1]
[^1]: Copied directly from the [GW2 Wiki](https://wiki.guildwars2.com/wiki/API:2/account)
```json
{
  "id": "C19467C6-F5AD-E211-8756-78E7D1936222",
  "name": "Account.1234",
  "age": 22911780,
  "world": 1004,
  "guilds": [
    "116E0C0E-0035-44A9-BB22-4AE3E23127E5",
    "5AE2FE0C-79B2-4AA9-8A03-80CBE3A3740D",
    "A0F09951-FBA2-492E-8888-C449C217ECAD",
    "8977C915-D948-E511-8D0D-AC162DAE8ACD",
    "032AAA16-749B-E311-A32A-E4115BDFA895"
  ],
  "guild_leader": [
    "032AAA16-749B-E311-A32A-E4115BDFA895"
  ],
  "created": "2013-04-25T22:09:00Z",
  "access": [
    "GuildWars2",
    "HeartOfThorns",
    "PathOfFire"
  ],
  "commander": true,
  "fractal_level": 100,
  "daily_ap": 7659,
  "monthly_ap": 1129,
  "wvw_rank": 514
}
```
**Exceptions**
This line might throw some exceptions
```csharp
var queryResult = await apiManager.ResolveQuery(query);
```
- `ApiParser.EndpointRequestException` with an inner `Gw2Sharp.WebApi.Exceptions.InvalidAccessTokenException`
 if the API connection was not made with a proper access token, since the Account Endpoint requires authentication

### Retrieve indexed data from the Account/Materials endpoint
```csharp
// construct the query
// when using indices, a type identifier needs to be provided
var query = EndpointQuery.FromString("Account.Materials[INT:2]");

// instantiate the apiManager
// assumes client to be a Gw2Sharp.Gw2Client
// will not refresh the cached data, if a request is made for 2 minutes since the last refresh.
var apiManager = new ApiManager(client.WebApi.V2, new ApiParser.V2.Settings.ApiManagerSettings() { Cooldown = 120_000 });

// might throw exceptions
var queryResult = await apiManager.ResolveQuery(query);

// check if the result contains the expected data
if (queryResult is Gw2Sharp.WebApi.V2.Models.AccountMaterial materialData)
{
	// do stuff
}
```
**Result**
The result is a `Gw2Sharp.WebApi.V2.Models.AccountMaterial` object. For reference see
 the [Gw2Sharp implementation](https://github.com/Archomeda/Gw2Sharp/blob/master/Gw2Sharp/WebApi/V2/Models/Account/AccountMaterial.cs).

The underlying API response might look a little bit like this[^2]
[^2]: Copied directly from the [GW2 Wiki](https://wiki.guildwars2.com/wiki/API:2/account/materials)
```json
{
    "id": 12134,
    "category": 5,
    "count": 64
}
```
**Exceptions**
This line might throw some exceptions
```csharp
var queryResult = await apiManager.ResolveQuery(query);
```
- `ApiParser.EndpointRequestException` with an inner `Gw2Sharp.WebApi.Exceptions.InvalidAccessTokenException`
 if the API connection was not made with a proper access token, since the Account/Materials Endpoint requires authentication

### Retrieve indexed data from the restricted Guild/:id/Members endpoint
```csharp
// construct the query
// when using indices, a type identifier needs to be provided
var query = EndpointQuery.FromString($"Guild[GUID:{"116E0C0E-0035-44A9-BB22-4AE3E23127E5"}].Members[INT:0].Joined", null);

// instantiate the apiManager
// assumes client to be a Gw2Sharp.Gw2Client
// will not refresh the cached data, if a request is made for 2 minutes since the last refresh.
var apiManager = new ApiManager(client.WebApi.V2, new ApiParser.V2.Settings.ApiManagerSettings() { Cooldown = 120_000 });

// might throw some exceptions
var queryResult = await apiManager.ResolveQuery(query);

// check if the result contains the expected data
if (queryResult is System.DateTimeOffset)
{
	// do stuff
}
```
**Result**
The result is a `System.DateTimeOffset` object. For reference see
 the [Microsoft Documentation](https://learn.microsoft.com/en-us/dotnet/api/system.datetimeoffset?view=netframework-4.8).

The underlying API response might look a little bit like this[^3]
[^3]: Copied directly from the [GW2 Wiki](https://wiki.guildwars2.com/wiki/API:2/guild/:id/members)
```json
{
    name: "Lawton Campbell.9413",
    rank: "Leader",
    joined: "2015-07-22T06:18:35.000Z"
}
```
**Exceptions**
This line might throw some exceptions
```csharp
var queryResult = await apiManager.ResolveQuery(query);
```
- `ApiParser.EndpointRequestException` with an inner `Gw2Sharp.WebApi.Exceptions.InvalidAccessTokenException`
 if the API connection was not made with a proper access token, since the Account/Materials Endpoint requires authentication
- `ApiParser.EndpointRequestException` with an inner `Gw2Sharp.WebApi.Exceptions.Gw2Sharp.WebApi.Exceptions.MembershipRequiredException`
 if the used access token is from a user, that is not part of the guild
- `ApiParser.EndpointRequestException` with an inner `Gw2Sharp.WebApi.Exceptions.RestrictedToGuildLeadersException`
 if the used access token is from a user, that is part of the guild, but not the guild leader
