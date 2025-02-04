import json
import os
from s2ms_cluster import WORKSPACE_ENDPOINT_FILE

NET_FRAMEWORKS = ["net462", "net472", "netcoreapp3.1", "net6.0", "net7.0", "net8.0"]


if __name__ == "__main__":

    home_dir = os.getenv("HOMEPATH")
    if home_dir is None:
        home_dir = os.getenv("HOME")

    with open(WORKSPACE_ENDPOINT_FILE, "r") as f:
        hostname = f.read()
    password = os.getenv("SQL_USER_PASSWORD")

    with open("./.circleci/SideBySide/config.json", "r") as f_in:
        config_content = f_in.read()

    config_content = config_content.replace("SINGLESTORE_HOST", hostname, 1)
    config_content = config_content.replace("SQL_USER_PASSWORD", password, 1)
    config_content = config_content.replace("SQL_USER_NAME", "admin", 1)

    for target_framework in NET_FRAMEWORKS:
        with open(f"/home/circleci/project/artifacts/bin/SideBySide/release_{target_framework}/config.json", "w") as f_out:
            f_out.write(config_content)

    with open(os.path.join(home_dir, "CONNECTION_STRING"), "w") as f_conn:
        f_conn.write(json.loads(config_content)["Data"]["ConnectionString"])
