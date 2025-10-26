{{/*
Expand the name of the chart.
*/}}
{{- define "loopai.name" -}}
{{- default .Chart.Name .Values.nameOverride | trunc 63 | trimSuffix "-" }}
{{- end }}

{{/*
Create a default fully qualified app name.
*/}}
{{- define "loopai.fullname" -}}
{{- if .Values.fullnameOverride }}
{{- .Values.fullnameOverride | trunc 63 | trimSuffix "-" }}
{{- else }}
{{- $name := default .Chart.Name .Values.nameOverride }}
{{- if contains $name .Release.Name }}
{{- .Release.Name | trunc 63 | trimSuffix "-" }}
{{- else }}
{{- printf "%s-%s" .Release.Name $name | trunc 63 | trimSuffix "-" }}
{{- end }}
{{- end }}
{{- end }}

{{/*
Create chart name and version as used by the chart label.
*/}}
{{- define "loopai.chart" -}}
{{- printf "%s-%s" .Chart.Name .Chart.Version | replace "+" "_" | trunc 63 | trimSuffix "-" }}
{{- end }}

{{/*
Common labels
*/}}
{{- define "loopai.labels" -}}
helm.sh/chart: {{ include "loopai.chart" . }}
{{ include "loopai.selectorLabels" . }}
{{- if .Chart.AppVersion }}
app.kubernetes.io/version: {{ .Chart.AppVersion | quote }}
{{- end }}
app.kubernetes.io/managed-by: {{ .Release.Service }}
{{- end }}

{{/*
Selector labels
*/}}
{{- define "loopai.selectorLabels" -}}
app.kubernetes.io/name: {{ include "loopai.name" . }}
app.kubernetes.io/instance: {{ .Release.Name }}
{{- end }}

{{/*
Create the name of the service account to use
*/}}
{{- define "loopai.serviceAccountName" -}}
{{- if .Values.serviceAccount.create }}
{{- default (include "loopai.fullname" .) .Values.serviceAccount.name }}
{{- else }}
{{- default "default" .Values.serviceAccount.name }}
{{- end }}
{{- end }}

{{/*
Database connection string
*/}}
{{- define "loopai.databaseConnectionString" -}}
Server={{.Values.database.host}},{{.Values.database.port}};Database={{.Values.database.name}};User Id={{.Values.database.user}};Password=$(DB_PASSWORD);TrustServerCertificate=True;
{{- end }}

{{/*
Redis connection string
*/}}
{{- define "loopai.redisConnectionString" -}}
{{- if .Values.redis.enabled }}
{{.Values.redis.host}}:{{.Values.redis.port}},password=$(REDIS_PASSWORD)
{{- end }}
{{- end }}
