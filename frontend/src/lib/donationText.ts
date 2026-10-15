const SEPARATOR = ', '

/** Soma uma sugestão ("Arroz") à descrição da doação sem repetir o que já foi escrito. */
export function appendSuggestion(description: string, suggestion: string): string {
  const current = description.trim().replace(/[,;]\s*$/, '')
  if (current === '') return suggestion

  const alreadyMentioned = current.toLowerCase().includes(suggestion.toLowerCase())
  return alreadyMentioned ? current : `${current}${SEPARATOR}${suggestion}`
}
