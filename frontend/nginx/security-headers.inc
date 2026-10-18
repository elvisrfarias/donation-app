# Cabeçalhos de segurança do frontend (incluído em cada location com add_header próprio).
# CSP: só scripts do próprio domínio; estilos inline são exigidos por Leaflet/Recharts.
# Origens externas: tiles do OpenStreetMap, Nominatim (geocodificação), BrasilAPI (CEP),
# o storage público das imagens (${STORAGE_PUBLIC_URL}) e o Web Analytics da Cloudflare,
# que a própria Cloudflare injeta na página. A fonte é servida pelo próprio site.
add_header Content-Security-Policy "default-src 'self'; script-src 'self' https://static.cloudflareinsights.com; style-src 'self' 'unsafe-inline'; font-src 'self'; img-src 'self' data: blob: https://tile.openstreetmap.org ${STORAGE_PUBLIC_URL}; connect-src 'self' https://nominatim.openstreetmap.org https://brasilapi.com.br https://cloudflareinsights.com; worker-src 'self'; manifest-src 'self'; object-src 'none'; base-uri 'self'; form-action 'self'; frame-ancestors 'none'" always;
add_header X-Content-Type-Options "nosniff" always;
add_header X-Frame-Options "DENY" always;
add_header Referrer-Policy "strict-origin-when-cross-origin" always;
add_header Permissions-Policy "geolocation=(self), camera=(), microphone=(), payment=(), usb=()" always;
add_header Cross-Origin-Opener-Policy "same-origin" always;
