import { Search } from 'lucide-react'
import { useState, type FormEvent } from 'react'
import { isCompleteCpf, maskCpf } from '@/lib/cpfInput'
import { Alert, Button, FormField, Input } from '@/views/components/ui'

interface CpfLookupProps {
  disabled: boolean
  disabledHint?: string
  isSearching: boolean
  errorMessage: string | null
  onSearch: (cpf: string) => void
}

export function CpfLookup({
  disabled,
  disabledHint,
  isSearching,
  errorMessage,
  onSearch,
}: CpfLookupProps) {
  const [cpf, setCpf] = useState('')

  function submit(event: FormEvent) {
    event.preventDefault()
    if (isCompleteCpf(cpf) && !disabled) onSearch(cpf)
  }

  return (
    <form onSubmit={submit} className="space-y-3">
      <FormField
        label="CPF do beneficiário"
        htmlFor="withdrawal-cpf"
        hint={disabled ? disabledHint : 'Digite os 11 números e pressione Enter.'}
      >
        <div className="flex flex-wrap gap-2">
          <Input
            id="withdrawal-cpf"
            autoFocus
            inputMode="numeric"
            autoComplete="off"
            placeholder="000.000.000-00"
            className="max-w-xs min-w-48 flex-1 text-base tabular-nums"
            value={cpf}
            disabled={disabled}
            onChange={(event) => setCpf(maskCpf(event.target.value))}
          />
          <Button
            type="submit"
            variant="brand"
            leftIcon={<Search className="size-4" />}
            loading={isSearching}
            disabled={disabled || !isCompleteCpf(cpf)}
          >
            Buscar
          </Button>
        </div>
      </FormField>
      {errorMessage && <Alert tone="warning">{errorMessage}</Alert>}
    </form>
  )
}
