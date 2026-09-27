import { useState } from 'react'
import { cleanup, render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { afterEach, beforeAll, describe, expect, it, vi } from 'vitest'
import { ComboBoxInput } from './ComboBoxInput'

const UNITS = ['kg', 'un', 'L', 'pacote', 'caixa', 'g']

beforeAll(() => {
  globalThis.ResizeObserver = class {
    observe() {}
    unobserve() {}
    disconnect() {}
  }
  Element.prototype.scrollIntoView = vi.fn()
})

afterEach(cleanup)

function ControlledComboBox({ onChange }: { onChange?: (v: string) => void }) {
  const [value, setValue] = useState('')
  return (
    <ComboBoxInput
      aria-label="Unidade"
      value={value}
      suggestions={UNITS}
      onValueChange={(next) => {
        setValue(next)
        onChange?.(next)
      }}
    />
  )
}

describe('ComboBoxInput', () => {
  it('mostra as sugestões ao focar, sem precisar digitar nada', async () => {
    render(<ControlledComboBox />)

    await userEvent.click(screen.getByRole('combobox'))

    expect(await screen.findByText('kg')).toBeInTheDocument()
    expect(screen.getByText('pacote')).toBeInTheDocument()
  })

  it('deixa digitar um valor livre, mesmo que não esteja nas sugestões', async () => {
    render(<ControlledComboBox />)

    await userEvent.type(screen.getByRole('combobox'), 'dúzia')

    expect(screen.getByRole('combobox')).toHaveValue('dúzia')
  })

  it('filtra as sugestões conforme digita', async () => {
    render(<ControlledComboBox />)

    await userEvent.type(screen.getByRole('combobox'), 'ca')

    expect(await screen.findByText('caixa')).toBeInTheDocument()
    expect(screen.queryByText('kg')).not.toBeInTheDocument()
  })

  it('escolhe uma sugestão ao clicar', async () => {
    const onChange = vi.fn()
    render(<ControlledComboBox onChange={onChange} />)

    await userEvent.click(screen.getByRole('combobox'))
    await userEvent.click(await screen.findByText('pacote'))

    expect(onChange).toHaveBeenCalledWith('pacote')
    expect(screen.getByRole('combobox')).toHaveValue('pacote')
  })
})
