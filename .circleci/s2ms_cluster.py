import json
import os
import pymysql
import requests
from requests.adapters import HTTPAdapter
import sys
from time import sleep
from typing import Dict, Optional
from urllib3 import Retry

BASE_URL = "https://api.singlestore.com"
CLUSTERS_PATH = "/v0beta/clusters"

SQL_USER_PASSWORD = os.getenv("SQL_USER_PASSWORD")  # project UI env-var reference
S2MS_API_KEY = os.getenv("S2MS_API_KEY")  # project UI env-var reference

HEADERS = {
    "Authorization": f"Bearer {S2MS_API_KEY}",
    "Content-Type": "application/json",
    "Accept": "application/json"
}

CLUSTER_NAME = ".NET-connector-ci-test-cluster"
AWS_EU_CENTRAL_REGION = "7e7ffd27-20f7-44b6-87e6-e72828a81ac7"
AUTO_TERMINATE_MINUTES = 30

PAYLOAD_FOR_CREATE = {
    "name": CLUSTER_NAME,
    "regionID": AWS_EU_CENTRAL_REGION,
    "adminPassword": SQL_USER_PASSWORD,
    "expiresAt": f"{AUTO_TERMINATE_MINUTES}m",
    "firewallRanges": [
        "0.0.0.0/0"
    ],
    "size": "S-00"
}
HOSTNAME_TMPL = "svc-{}-ddl.aws-frankfurt-1.svc.singlestore.com"
CLUSTER_ID_FILE = "CLUSTER_ID"

TOTAL_RETRIES = 5
S2MS_REQUEST_TIMEOUT = 60


def request_with_retry(request_method, url, data=None, headers=HEADERS):
    try:
        with requests.Session() as s:
            retries = Retry(
                total=TOTAL_RETRIES,
                backoff_factor=0.2,
                status_forcelist=[500, 502, 503, 504])

            s.mount('http://', HTTPAdapter(max_retries=retries))
            s.mount('https://', HTTPAdapter(max_retries=retries))

            return s.request(request_method, url, data=data, headers=headers, timeout=S2MS_REQUEST_TIMEOUT)
    except requests.exceptions.RequestException as e:
        raise SystemExit(e)


def create_cluster() -> str:
    cl_id_response = request_with_retry("POST", BASE_URL + CLUSTERS_PATH, data=json.dumps(PAYLOAD_FOR_CREATE))
    cl_id_response.raise_for_status()
    return cl_id_response.json()["clusterID"]


def get_cluster_info(cluster_id: str) -> Dict:
    cl_id = request_with_retry("GET", BASE_URL + CLUSTERS_PATH + f"/{cluster_id}")
    return cl_id.json()


def is_cluster_active(cluster_id: str) -> bool:
    cl_info = get_cluster_info(cluster_id)
    return cl_info["state"] == "Active"


def wait_start(cluster_id: str) -> None:
    print(f"Waiting for cluster {cluster_id} to be available for connection..", end="", flush=True)
    time_wait = 0
    while (not is_cluster_active(cluster_id) and time_wait < 600):
        print(".", end="", flush=True)
        sleep(5)
        time_wait += 5
    if time_wait < 600:
        print("\nCluster is active!")
    else:
        print(f"\nTimeout error: can't connect to {cluster_id} for more than 10 minutes!")


def terminate_cluster(cluster_id: str) -> None:
    request_with_retry("DELETE", BASE_URL + CLUSTERS_PATH + f"/{cluster_id}")


def check_connection(cluster_id: str, create_db: Optional[str] = None):
    conn = pymysql.connect(
        user="admin",
        password=SQL_USER_PASSWORD,
        host=HOSTNAME_TMPL.format(cluster_id),
        port=3306)

    cur = conn.cursor()
    try:
        cur.execute("SELECT NOW():>TEXT")
        res = cur.fetchall()
        print(f"Successfully connected to {cluster_id} at {res[0][0]}")

        if create_db is not None:
            cur.execute(f"DROP DATABASE IF EXISTS {create_db}")
            cur.execute(f"CREATE DATABASE {create_db}")
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

    if command == "start":
        new_cl_id = create_cluster()
        with open(CLUSTER_ID_FILE, "w") as f:
            f.write(new_cl_id)
        wait_start(new_cl_id)
        check_connection(new_cl_id, db_name)
        exit(0)

    if command == "terminate":
        with open(CLUSTER_ID_FILE, "r") as f:
            cl_id = f.read()
        terminate_cluster(cl_id)
        exit(0)
