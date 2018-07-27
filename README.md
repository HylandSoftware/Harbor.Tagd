# Tag Cleanup Daemon for [VMware Harbor](https://github.com/vmware/harbor)

[![Build Status](https://travis-ci.org/HylandSoftware/Harbor.Tagd.svg?branch=master)](https://travis-ci.org/HylandSoftware/Harbor.Tagd) [![Coverage Status](https://coveralls.io/repos/github/HylandSoftware/Harbor.Tagd/badge.svg?branch=master)](https://coveralls.io/github/HylandSoftware/Harbor.Tagd?branch=master)

`tagd` automates the process of cleaning up old tags from your Harbor container
registries. You can override the default policy to be more or less specific for
projects and repositories if needed.

## Building

You need version 2.0 or later of the dotnet core SDK. macOS and Linux also
require `mono` to be installed to run the build script, but the project could
be built manually.

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

The easiest way to get up and running is to use the Docker Container:

```bash
docker run -it --rm -v config.yml:/config.yml hcr.io/nlowe/tagd \
    --config-file /config.yml \
    --endpoint <your harbor server> \
    --user <service user> \
    --password <service user password>
```

This will perform a dry run using the rules specified in `config.yml`. To
actually delete tags, invoke with `--destructive`.

### Usage

```text
Usage: Harbor.Tagd [ -h|--help ] [ --version ] --endpoint -u|--userU -p|--passwordP [ -v|--verbosity V ] [ --config-file  ] [ --config-server  ] [ --config-user  ] [ --config-password  ] [ --destructive ] [ --dump-rules ] [ --notify-slack  ]

 Tag Cleanup daemon for VMware Harbor Registries

Required Arguments:
 --endpoint         The harbor registry to connect to
 -p, --password     The password for the user connecting to harbor
 -u, --user         The user to connect to harbor as

Optional Arguments:
 --config-file      The config file to parse
 --config-server    The springboot config server to get configuration from
 --config-user      The user to login to the springboot config server as
 --config-password  The password for the springboot config server user
 --destructive      Actually delete tags instead of generating a report
 --dump-rules       Print the rules that would be used and exit
 --notify-slack     Post results to this slack-compatible webhook
 -h, --help         Display this help document.
 -v, --verbosity    How verbose should logging output be
 --version          Displays the version of the current executable.
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

## License

tagd is licensed under the MIT License. See [`LICENSE`](./LICENSE) for details.

tagd makes use of [`nuget`](https://nuget.org) for package management. Packages
restored by `nuget` have their own license which may differ from the terms of
the MIT license that we use.
