# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added

- Cluster.Namespace
- Workload.Job
- Workload.CronJob
- Workload.Pod
	- restartPolicy

### Fixed

- ServiceAccount and ClusterRoleBinding not recognised by K8s

## [0.0.1] - 2022-11-13

### Added

- Common.ContainerPort
- Common.EnvVar
- Common.LabelSelector
- Common.ObjectReference
- Common.Metadata
- Common.Protocol
- Common.ResourceList
- Common. VolumeMount
- Workload.Pd
- Workload.Deployment
- Authentication.ServiceAccount
- Authorization.ClusterRoleBinding
- Authorization.RoleRef
- Authorization.Subject
- Authorization.Ingress
- Service.Service
- Service.Service
- Storage.ConfigMap
- Storage.Secret
- Storage.PersistentVolumeClaim
- Storage.StorageClass
- Storage.PersistentVolumes
  - PersistentVolumeClaimVolume
  - ConfigMapVolume
  - SecretVolume
  - EmptyDirVolume
  - HostPathVolume
  - CSIVolume
- Basic validation of resources
- JSON serialization and file write
- YAML serialization and file write