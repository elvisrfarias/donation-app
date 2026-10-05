import { useEffect, useRef, useState } from 'react'
import type { StockItem } from '@/models/types'
import { useUpdateStockQuantities } from '@/viewmodels'

const DEBOUNCE_MS = 450
const SAVED_FLASH_MS = 2000

export type AutoSaveStatus = 'idle' | 'saving' | 'saved' | 'error'

/**
 * Salva as quantidades de estoque sozinho, sem botão de salvar: cada mudança agenda um envio; se
 * vier mais mudança antes de disparar, elas se juntam num único PUT em lote (mesma rota que já
 * existia para o salvamento em bloco). Nunca há duas requisições de quantidade em voo ao mesmo
 * tempo — evita uma resposta atrasada sobrescrever uma edição mais nova de outro item.
 */
export function useAutoSaveStockQuantities(pointId: string, items: StockItem[]) {
  const { updateStockQuantities } = useUpdateStockQuantities()
  const [status, setStatus] = useState<AutoSaveStatus>('idle')
  const [errorMessage, setErrorMessage] = useState<string | null>(null)
  const [overrides, setOverrides] = useState<Record<string, number>>({})

  const pendingRef = useRef<Record<string, number>>({})
  const flushingRef = useRef(false)
  const debounceRef = useRef<ReturnType<typeof setTimeout> | null>(null)
  const savedFlashRef = useRef<ReturnType<typeof setTimeout> | null>(null)

  useEffect(
    () => () => {
      if (debounceRef.current) clearTimeout(debounceRef.current)
      if (savedFlashRef.current) clearTimeout(savedFlashRef.current)
    },
    [],
  )

  useEffect(() => {
    setOverrides((previous) => {
      let changed = false
      const next = { ...previous }
      for (const item of items) {
        if (
          item.id in next &&
          !(item.id in pendingRef.current) &&
          next[item.id] === item.quantity
        ) {
          delete next[item.id]
          changed = true
        }
      }
      return changed ? next : previous
    })
  }, [items])

  async function flush() {
    if (flushingRef.current) return
    const toSend = pendingRef.current
    const entries = Object.entries(toSend)
    if (entries.length === 0) return

    flushingRef.current = true
    pendingRef.current = {}
    setStatus('saving')

    try {
      await updateStockQuantities({
        pointId,
        input: { items: entries.map(([stockItemId, quantity]) => ({ stockItemId, quantity })) },
      })
      setErrorMessage(null)
      setStatus('saved')
      if (savedFlashRef.current) clearTimeout(savedFlashRef.current)
      savedFlashRef.current = setTimeout(() => setStatus('idle'), SAVED_FLASH_MS)
      flushingRef.current = false
      // Só continua sozinho quando há mudanças novas acumuladas durante o envio — nunca tenta de
      // novo automaticamente depois de uma falha (isso bombardearia a API sem parar); a próxima
      // mudança do usuário ou um saveNow() explícito retoma o envio, agora incluindo o que falhou.
      if (Object.keys(pendingRef.current).length > 0) {
        void flush()
      }
    } catch (error) {
      pendingRef.current = { ...toSend, ...pendingRef.current }
      setStatus('error')
      setErrorMessage(error instanceof Error ? error.message : 'Não foi possível salvar o estoque.')
      flushingRef.current = false
    }
  }

  function quantityFor(item: StockItem): number {
    return overrides[item.id] ?? item.quantity
  }

  function setQuantity(itemId: string, quantity: number) {
    setOverrides((previous) => ({ ...previous, [itemId]: quantity }))
    pendingRef.current[itemId] = quantity
    if (debounceRef.current) clearTimeout(debounceRef.current)
    debounceRef.current = setTimeout(() => void flush(), DEBOUNCE_MS)
  }

  function saveNow() {
    if (debounceRef.current) clearTimeout(debounceRef.current)
    void flush()
  }

  return { quantityFor, setQuantity, saveNow, status, errorMessage }
}
