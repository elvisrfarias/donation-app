/** Escapa um valor para uma célula CSV (aspas e separadores seguros). */
function toCell(value: unknown): string {
  const text = value == null ? '' : String(value)
  return /[",\n;]/.test(text) ? `"${text.replace(/"/g, '""')}"` : text
}

/** BOM UTF-8 (U+FEFF) para o Excel abrir a acentuação corretamente. */
const UTF8_BOM = String.fromCharCode(0xfeff)

/** Monta o conteúdo CSV a partir de cabeçalhos e linhas (usa `;`, padrão pt-BR/Excel). */
export function buildCsv(headers: string[], rows: unknown[][]): string {
  const lines = [headers, ...rows].map((row) => row.map(toCell).join(';'))
  return `${UTF8_BOM}${lines.join('\r\n')}`
}

/** Gera e baixa um arquivo CSV no navegador. */
export function downloadCsv(filename: string, headers: string[], rows: unknown[][]): void {
  const blob = new Blob([buildCsv(headers, rows)], { type: 'text/csv;charset=utf-8;' })
  const url = URL.createObjectURL(blob)
  const link = document.createElement('a')
  link.href = url
  link.download = filename.endsWith('.csv') ? filename : `${filename}.csv`
  document.body.appendChild(link)
  link.click()
  document.body.removeChild(link)
  URL.revokeObjectURL(url)
}
