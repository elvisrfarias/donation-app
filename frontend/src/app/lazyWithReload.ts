import { lazy, type ComponentType } from 'react'

// Após um deploy, os chunks por rota ganham um novo hash no nome
// (ex.: AppLayout-CECKAH6X.js -> AppLayout-Dv7zYaep.js). Uma aba já aberta
// ainda referencia o hash antigo; ao navegar, o `import()` dispara um 404 e o
// React Router mostra "Failed to fetch dynamically imported module".
//
// A recuperação é recarregar a página *uma vez* para buscar o index.html novo
// (que aponta para os hashes atuais). O flag em sessionStorage evita loop de
// reload caso a falha seja real (offline, chunk de fato ausente) — nesse caso
// o erro propaga e cai no errorElement da rota.
export function lazyWithReload<T extends ComponentType<unknown>>(
  factory: () => Promise<{ default: T }>,
) {
  const RELOAD_KEY = 'chunk-reload-attempted'

  return lazy(async () => {
    try {
      const mod = await factory()
      sessionStorage.removeItem(RELOAD_KEY)
      return mod
    } catch (error) {
      const alreadyReloaded = sessionStorage.getItem(RELOAD_KEY) === '1'
      if (!alreadyReloaded) {
        sessionStorage.setItem(RELOAD_KEY, '1')
        window.location.reload()
        // Devolve uma Promise pendente: a página recarrega antes de resolver.
        return new Promise<{ default: T }>(() => {})
      }
      throw error
    }
  })
}
