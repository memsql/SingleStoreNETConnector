import os
import singlestoredb as s2
import uuid
import sys
import time
from typing import Dict, Optional

SQL_USER_PASSWORD = os.getenv("SQL_USER_PASSWORD")  # project UI env-var reference
S2MS_API_KEY = os.getenv("S2MS_API_KEY")  # project UI env-var reference

WORKSPACE_GROUP_BASE_NAME = ".NET-connector-ci-test-cluster"
WORKSPACE_NAME = "tests"
AWS_US_WEST_REGION = "1c1de314-2cc0-4c74-bd54-5047ff90842e"
AUTO_TERMINATE_MINUTES = 60
WORKSPACE_ENDPOINT_FILE = "WORKSPACE_ENDPOINT_FILE"
WORKSPACE_GROUP_ID_FILE = "WORKSPACE_GROUP_ID_FILE"

TOTAL_RETRIES = 5

def retry(func):
     for i in range(TOTAL_RETRIES):
        try:
            return func()
        except Exception as e:
            if i == TOTAL_RETRIES - 1:
                raise
            print(f"Attempt {i+1} failed with error: {e}.")


def create_workspace(workspace_manager):
    w_group_name = WORKSPACE_GROUP_BASE_NAME + "-" + uuid.uuid4().hex
    def create_workspace_group():
        return workspace_manager.create_workspace_group(
            name=w_group_name,
            region=AWS_US_WEST_REGION,
            firewall_ranges=["0.0.0.0/0"],
            admin_password=SQL_USER_PASSWORD,
            expires_at="60m"
        )
    workspace_group = retry(create_workspace_group)

    with open(WORKSPACE_GROUP_ID_FILE, "w") as f:
        f.write(workspace_group.id)
    print("Created workspace group {}".format(w_group_name))

    workspace = workspace_group.create_workspace(name=WORKSPACE_NAME, size="S-00", wait_on_active=True, wait_timeout=600)

    with open(WORKSPACE_ENDPOINT_FILE, "w") as f:
        f.write(workspace.endpoint)

    return workspace


def terminate_workspace(workspace_manager) -> None:
    with open(WORKSPACE_GROUP_ID_FILE, "r") as f:
        workspace_group_id = f.read()
    workspace_group = workspace_manager.get_workspace_group(workspace_group_id)

    for workspace in workspace_group.workspaces:
        workspace.terminate(wait_on_terminated=True)
    workspace_group.terminate()


def check_and_update_connection(create_db: Optional[str] = None):
    with open(WORKSPACE_GROUP_ID_FILE, "r") as f:
        workspace_group_id = f.read()
    workspace_group = workspace_manager.get_workspace_group(workspace_group_id)
    workspace = workspace_group.workspaces[0]

    def connect_to_workspace():
        return workspace.connect(user="admin", password=SQL_USER_PASSWORD, port=3306)
    conn = retry(connect_to_workspace)

    cur = conn.cursor()
    try:
        cur.execute("SELECT NOW():>TEXT")
        res = cur.fetchall()
        print(f"Successfully connected to {workspace.id} at {res[0][0]}")

        if create_db is not None:
            cur.execute(f"DROP DATABASE IF EXISTS {create_db}")
            cur.execute(f"CREATE DATABASE {create_db}")
            cur.execute("SET GLOBAL data_conversion_compatibility_level = '6.0'")
    finally:
        cur.close()
        conn.close()


if __name__ == "__main__":
    if len(sys.argv) < 2:
        print("Not enough arguments to start/terminate cluster!")
        exit(1)
    command = sys.argv[1]
    db_name = None
    if len(sys.argv) > 2:
        db_name = sys.argv[2]

    workspace_manager = s2.manage_workspaces(access_token=S2MS_API_KEY)

    if command == "start":
        create_workspace(workspace_manager)
        check_and_update_connection(db_name)
        exit(0)

    if command == "terminate":
        terminate_workspace(workspace_manager)
        exit(0)

    if command == "update":
        check_and_update_connection(db_name)
        exit(0)

