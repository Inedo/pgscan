# pgscan is now pgutil

As of ProGet 2024, `pgscan` has been retired and incorporated into [pgutil](https://docs.inedo.com/docs/proget/api/pgutil), our command line tool that provides a variety of commands to upload/download packages, manage feeds, audit package compliance, assess vulnerabilities, etc. 

While `pgscan identify` will still work, you should update to use `pgutil builds scan`. It works almost the same: 

Before:
```
pgscan identify --input=MyLibrary.csproj --proget-url=https://proget.local --version=1.0.0
```

After:
```
pgutil builds scan --project-name="myProject" --version=1.2.3 --source=https://proget.local
```


See [pgutil builds scan](https://docs.inedo.com/docs/proget/api/sca/builds/scan) to learn more or the [old version of this README.md](https://github.com/Inedo/pgscan/blob/b7ea10a293a9d16162c18270747d8a8f7f148db3/README.md),
