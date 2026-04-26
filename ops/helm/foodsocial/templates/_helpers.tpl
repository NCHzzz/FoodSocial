{{- define "foodsocial.name" -}}
foodsocial
{{- end -}}

{{- define "foodsocial.labels" -}}
app.kubernetes.io/part-of: foodsocial
app.kubernetes.io/managed-by: Helm
helm.sh/chart: {{ .Chart.Name }}-{{ .Chart.Version | replace "+" "_" }}
{{- end -}}

{{- define "foodsocial.serviceImage" -}}
{{- $root := index . "root" -}}
{{- $service := index . "service" -}}
{{- printf "%s/%s/%s:%s" $root.Values.global.imageRegistry $root.Values.global.imageRepository $service.image $root.Values.global.imageTag -}}
{{- end -}}
