# Repasse das páginas renderizadas pela API (incluído nas locations de SEO). Cada location
# define o próprio error_page para cair no arquivo estático se a API falhar.
proxy_http_version 1.1;
proxy_set_header Host $host;
proxy_set_header X-Real-IP $remote_addr;
proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
proxy_set_header X-Forwarded-Proto $forwarded_proto;
proxy_connect_timeout 3s;
proxy_read_timeout 10s;
proxy_intercept_errors on;
