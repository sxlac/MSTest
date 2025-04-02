FROM maven:3.8.5-jdk-11-slim
ENV TZ=America/Chicago
RUN mkdir -p /usr/src/app
WORKDIR /usr/src/app
COPY pom.xml /usr/src/app/
COPY ./src/test /usr/src/app/src/test
CMD mvn validate
