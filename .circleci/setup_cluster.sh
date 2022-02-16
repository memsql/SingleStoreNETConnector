#!/usr/bin/env bash
set -eu

DEFAULT_IMAGE_NAME="memsql/cluster-in-a-box:centos-7.3.9-a7abc2ebd4-3.2.8-1.11.4"
IMAGE_NAME="${SINGLESTORE_IMAGE:-$DEFAULT_IMAGE_NAME}"
CONTAINER_NAME="singlestore-integration"

EXISTS=$(docker inspect ${CONTAINER_NAME} >/dev/null 2>&1 && echo 1 || echo 0)

if [[ "${EXISTS}" -eq 1 ]]; then
  EXISTING_IMAGE_NAME=$(docker inspect -f '{{.Config.Image}}' ${CONTAINER_NAME})
  if [[ "${IMAGE_NAME}" != "${EXISTING_IMAGE_NAME}" ]]; then
    echo "Existing container ${CONTAINER_NAME} has image ${EXISTING_IMAGE_NAME} when ${IMAGE_NAME} is expected; recreating container."
    docker rm -f ${CONTAINER_NAME}
    EXISTS=0
  fi
fi

if [[ "${EXISTS}" -eq 0 ]]; then
    docker run -i --init \
        --name ${CONTAINER_NAME} \
        -e LICENSE_KEY=${LICENSE_KEY} \
        -e ROOT_PASSWORD=${SQL_USER_PASSWORD} \
        -p 3306:3306 -p 3307:3307 \
        ${IMAGE_NAME}
fi

docker start ${CONTAINER_NAME}

singlestore-wait-start() {
  echo -n "Waiting for SingleStore to start..."
  while true; do
      if mysql -u root -h 127.0.0.1 -P 3306 -p"${SQL_USER_PASSWORD}" -e "select 1" >/dev/null 2>/dev/null; then
          break
      fi
      echo -n "."
      sleep 0.2
  done
  mysql -u root -h 127.0.0.1 -P 3306 -p"${SQL_USER_PASSWORD}" -e "create database if not exists singlestoretest" >/dev/null 2>/dev/null
  echo ". Success!"
}

singlestore-wait-start

echo
echo "Ensuring child nodes are connected using container IP"
CONTAINER_IP=$(docker inspect -f '{{range .NetworkSettings.Networks}}{{.IPAddress}}{{end}}' ${CONTAINER_NAME})
CURRENT_LEAF_IP=$(mysql -u root -h 127.0.0.1 -P 3306 -p"${SQL_USER_PASSWORD}" --batch -N -e 'select host from information_schema.leaves')
if [[ ${CONTAINER_IP} != "${CURRENT_LEAF_IP}" ]]; then
    # remove leaf with current ip
    mysql -u root -h 127.0.0.1 -P 3306 -p"${SQL_USER_PASSWORD}" --batch -N -e "remove leaf '${CURRENT_LEAF_IP}':3307"
    # add leaf with correct ip
    mysql -u root -h 127.0.0.1 -P 3306 -p"${SQL_USER_PASSWORD}" --batch -N -e "add leaf root:'${SQL_USER_PASSWORD}'@'${CONTAINER_IP}':3307"
fi
echo "Done!"
