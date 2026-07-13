/** Logger central do app — encapsula o `console` para permitir trocar o destino (ex.: telemetria) num só lugar. */
export const logger = {
  warn(message: string, context?: Record<string, unknown>): void {
    console.warn(message, context ?? '')
  },
  error(message: string, context?: Record<string, unknown>): void {
    console.error(message, context ?? '')
  },
}
