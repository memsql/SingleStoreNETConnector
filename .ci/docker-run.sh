#!/bin/bash
cd $(dirname $0)

display_usage() {
    echo -e "\nUsage:\n$0 [image] [name] [port] [omit_features]\n"
}

# check whether user had supplied -h or --help . If yes display usage
if [[ ( $# == "--help") ||  $# == "-h" ]]
then
    display_usage
    exit 0
fi

# check number of arguments
if [ $# -ne 3 ]
then
    display_usage
    exit 1
fi

IMAGE=$1
PORT=$2
OMIT_FEATURES=$3
MYSQL_EXTRA=

if [ "$IMAGE" == "mysql:8.0" ]; then
  MYSQL_EXTRA='--default-authentication-plugin=mysql_native_password'
fi

sudo mkdir -p run/mysql
sudo chmod 777 run/mysql

docker run -d \
	-v $(pwd)/run/mysql:/var/run/mysqld:rw \
	-v $(pwd)/server:/etc/mysql/conf.d:ro \
	-p $PORT:3306 \
	--name mysql \
	-e MYSQL_ROOT_PASSWORD='test' \
	$IMAGE \
  --log-bin-trust-function-creators=1 \
  --local-infile=1 \
  --secure-file-priv=/var/tmp \
  --max-connections=250 \
  $MYSQL_EXTRA

for i in `seq 1 120`; do
	# wait for mysql to come up
	sleep 1
	echo "Testing if container is responding"
	docker exec mysql mysql -uroot -ptest -e "SELECT 1" >/dev/null 2>&1
	if [ $? -ne 0 ]; then continue; fi

	# try running the init script
	echo "Creating mysqltest user"
	docker exec mysql bash -c 'mysql -uroot -ptest < /etc/mysql/conf.d/init.sql'
	if [ $? -ne 0 ]; then continue; fi

	if [[ $OMIT_FEATURES != *"Sha256Password"* ]]; then
		echo "Creating sha256_password user"
	 	docker exec mysql bash -c 'mysql -uroot -ptest < /etc/mysql/conf.d/init_sha256.sql'
		if [ $? -ne 0 ]; then exit $?; fi
	fi

	if [[ $OMIT_FEATURES != *"CachingSha2Password"* ]]; then
		echo "Creating caching_sha2_password user"
		docker exec mysql bash -c 'mysql -uroot -ptest < /etc/mysql/conf.d/init_caching_sha2.sql'
		if [ $? -ne 0 ]; then exit $?; fi
	fi

	if [[ $OMIT_FEATURES != *"Ed25519"* ]]; then
		echo "Installing auth_ed25519 component"
		docker exec mysql bash -c 'mysql -uroot -ptest < /etc/mysql/conf.d/init_ed25519.sql'
		if [ $? -ne 0 ]; then exit $?; fi
	fi

	if [[ $OMIT_FEATURES != *"QueryAttributes"* ]]; then
		echo "Installing query_attributes component"
		docker exec mysql mysql -uroot -ptest -e "INSTALL COMPONENT 'file://component_query_attributes';"
		if [ $? -ne 0 ]; then exit $?; fi
	fi

	# exit if successful
	docker exec mysql mysql -ussltest -ptest \
		--ssl-mode=REQUIRED \
		--ssl-ca=/etc/mysql/conf.d/certs/ssl-ca-cert.pem \
		--ssl-cert=/etc/mysql/conf.d/certs/ssl-client-cert.pem \
		--ssl-key=/etc/mysql/conf.d/certs/ssl-client-key.pem \
		-e "SELECT 1"
	if [ $? -ne 0 ]; then
		# mariadb uses --ssl=TRUE instead of --ssl-mode=REQUIRED
		docker exec mysql mysql -ussltest -ptest \
			--ssl=TRUE \
			--ssl-ca=/etc/mysql/conf.d/certs/ssl-ca-cert.pem \
			--ssl-cert=/etc/mysql/conf.d/certs/ssl-client-cert.pem \
			--ssl-key=/etc/mysql/conf.d/certs/ssl-client-key.pem \
			-e "SELECT 1"
		if [ $? -ne 0 ]; then
			>&2 echo "Problem with SSL"
			exit 1
		fi
	fi
	echo "Ran Init Script"
	exit 0
done

# init script did not run
>&2 echo "Unable to Run Init Script"
exit 1
