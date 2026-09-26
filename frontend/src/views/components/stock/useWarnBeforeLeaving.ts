import { useEffect } from 'react'

export function useWarnBeforeLeaving(shouldWarn: boolean) {
  useEffect(() => {
    if (!shouldWarn) return
    const warn = (event: BeforeUnloadEvent) => event.preventDefault()
    window.addEventListener('beforeunload', warn)
    return () => window.removeEventListener('beforeunload', warn)
  }, [shouldWarn])
}
