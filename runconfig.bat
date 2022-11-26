@echo off
copy runconfig.toml server_config.toml
move server_config.toml bin/Content.Server/
pause
