# Tag Cleanup Daemon for [VMware Harbor](https://github.com/vmware/harbor)

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
    --endpoint <your harbor server>
    --username <service user>
    --password <service user password>
```

This will perform a dry run using the rules specified in `config.yml`. To
actually delete tags, invoke with `--report-only=false`.

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

**TBD**
