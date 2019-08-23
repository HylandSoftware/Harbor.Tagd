# Tag Cleanup Daemon for [VMware Harbor](https://github.com/vmware/harbor)

[![Build Status](https://travis-ci.org/HylandSoftware/Harbor.Tagd.svg?branch=master)](https://travis-ci.org/HylandSoftware/Harbor.Tagd) [![Coverage Status](https://coveralls.io/repos/github/HylandSoftware/Harbor.Tagd/badge.svg?branch=master)](https://coveralls.io/github/HylandSoftware/Harbor.Tagd?branch=master)

`tagd` automates the process of cleaning up old tags from your Harbor container
registries. You can override the default policy to be more or less specific for
projects and repositories if needed.

## Supported Versions

| `tagd` Version | `harbor` Version |
| -------------- | ---------------- |
| <= `1.7.2`     | `1.3.0` to `1.6` |
| `1.7.2`+       | Latest Public Release |

`tagd` works with Harbor `1.3.0` and later. Starting in `tagd` `1.7.2`,
`tagd` also supports `harbor` `1.7.0`+, and is required for versions of
`harbor` `1.7.0`+ due to a change in the Harbor API.

If you are running a non-release build of harbor, or if your harbor
installation blocks the `/api/systeminfo` endpoint, you will need to
set the `--login-behavior` flag appropriately:

| Flag | Description |
| --- | --- |
| `--login-behavior Probe` | Detect the version of harbor by probing the `/api/systeminfo` endpoint. This is the default behavior |
| `--login-behavior ForcePre17` | Assume the version of harbor is older than 1.7 |
| `--login-behavior ForcePost17` | Assume the version of harbor is 1.7 or newer |

## Building

You need version 2.0 or later of the dotnet core SDK.

Invoke the `cake` build script:

**Windows**:

```powershell
./build.ps1 -t Dist
```

**macOS / Linux**:

```bash
./build.sh -t Dist
```

This will create a `dist` folder in the current directory containing a published
copy of the application. Invoke with `dotnet ./dist/Harbor.Tagd.dll`. You can
also build a docker container by executing the `Docker` target for cake instead
of the `Dist` target.

## Running

The easiest way to get up and running is to use the [Docker Container](https://hub.docker.com/r/hylandsoftware/tagd/):

```bash
docker run -it --rm -v config.yml:/config.yml hylandsoftware/tagd \
    --config-file /config.yml \
    --endpoint <your harbor server> \
    --user <service user> \
    --password <service user password>
```

This will perform a dry run using the rules specified in `config.yml`. To
actually delete tags, invoke with `--destructive`.

### Usage

```text
Usage: Harbor.Tagd [ -h|--help ] [ --version ] --endpoint  -u|--user U -p|--password P [ --destructive ] [ --notify-slack  ] [ --login-behavior ] [ -v|--verbosity V ] [ --timeout  ] [ --config-file  ] [ --config-server  ] [ --config-user  ] [ --config-password  ] [ --insecure-disable-certificate-validation ]

 Tag Cleanup daemon for VMware Harbor Registries

Required Arguments:
 --endpoint                                 The harbor registry to connect to
 -p, --password                             The password for the user connecting to harbor
 -u, --user                                 The user to connect to harbor as

Optional Arguments:
 --destructive                              Actually delete tags instead of generating a report
 --notify-slack                             Post results to this slack-compatible webhook
 --login-behavior                           The login behavior to use. By default, tagd will try to determine what version of harbor it is connecting to in order to determine how to log in. Options: Probe, ForcePre17, ForcePost17
 --timeout                                  Timeout in seconds for http requests, defaults to 600
 --config-file                              The config file to parse
 --config-server                            The springboot config server to get configuration from
 --config-user                              The user to login to the springboot config server as
 --config-password                          The password for the springboot config server user
 --insecure-disable-certificate-validation  Don't validate server certificates for Harbor
 -h, --help                                 Display this help document.
 -v, --verbosity                            How verbose should logging output be
 --version                                  Displays the version of the current executable.
```

If no value is provided for `--password` you will be prompted to enter a masked password:

```bash
$ dotnet .\dist\Harbor.Tagd.dll --config-server https://config.mydomain.net --verbosity verbose --endpoint https://hcr.io --user harboradmin --password
Input a value for --password:
[17:17:26 INF] Fetching config from server at: https://config.mydomain.net
[17:17:27 INF] Located environment: tagd, ["Production"], null, 05ba9ab0fe7b8846da1863cac3b4eebaa260c54b
[17:17:27 INF] Loading rules using Harbor.Tagd.Rules.ConfigServerRuleProvider
[17:17:27 INF] Connecting to https://hcr.io as harboradmin
```

### Configuration

You can use a configuration file or connect to a [Spring CloudConfig Server](https://cloud.spring.io/spring-cloud-config/)
by specifying `--config-server` instead of `--config-file`. If your configuration
server requires authentication, provide `--config-user` and `--config-password`.

The backing file for the configuration server has the same format as
`--config-file`:

```yml
# Projects, Repositories, and Tags to globally ignore. Must match exactly
ignoreGlobally:
  projects: []
  repos: []
  tags: ['latest']
# The default rule to process if no other rules match
defaultRule:
  project: '.*'      # A regular expression matching a project
  repo: '.*'         # A regular Expression matching a repository
  tag: '.*'          # A regular Expression matching a tag
  ignore: ['latest'] # Tags to always keep. Must match exactly
  keep: 10           # The number of tags matching this rule to keep, sorted by creation date
# Additional Rules to process
rules:
- project: '.*'
  repo: '.*'
  tag: '.*'
  ignore: ['latest']
  keep: 10
```

You can validate your rule config using the `check` verb:

```bash
$ dotnet .\dist\Harbor.Tagd.dll check --config-file rules.yml --verbosity verbose
```

#### Usage

```bash
Usage: Harbor.Tagd [ -h|--help ] [ --version ] [ -v|--verbosity V ] [ --timeout  ] [ --config-file  ] [ --config-server  ] [ --config-user  ] [ --config-password  ] [ --insecure-disable-certificate-validation ]

 Load and validate rules

Optional Arguments:
 --timeout                                  Timeout in seconds for http requests, defaults to 600
 --config-file                              The config file to parse
 --config-server                            The springboot config server to get configuration from
 --config-user                              The user to login to the springboot config server as
 --config-password                          The password for the springboot config server user
 --insecure-disable-certificate-validation  Don't validate server certificates for Harbor
 -h, --help                                 Display this help document.
 -v, --verbosity                            How verbose should logging output be
 --version                                  Displays the version of the current executable.
```

## License

tagd is licensed under the MIT License. See [`LICENSE`](./LICENSE) for details.

tagd makes use of [`nuget`](https://nuget.org) for package management. Packages
restored by `nuget` have their own license which may differ from the terms of
the MIT license that we use.
