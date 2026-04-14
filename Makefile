APP_VERSION ?= v1.0.0
GIT_SHA     := $(shell git rev-parse --short HEAD)
TAG         := $(APP_VERSION)-$(GIT_SHA)

.PHONY: all build-all deploy-all

all: build-all deploy-all

build-all:
	eval $$(minikube docker-env) && APP_TAG=$(TAG) docker compose -f InternshipTracker/compose.yaml build

deploy-all:
	cd k8s && kustomize edit set image \
		internship-tracker/core-service=internship-tracker/core-service:$(TAG) \
		internship-tracker/user-service=internship-tracker/user-service:$(TAG) \
		internship-tracker/it-provision-service=internship-tracker/it-provision-service:$(TAG) \
		internship-tracker/notification-service=internship-tracker/notification-service:$(TAG) \
		internship-tracker/gateway-service=internship-tracker/gateway-service:$(TAG) \
		&& kubectl apply -k .
