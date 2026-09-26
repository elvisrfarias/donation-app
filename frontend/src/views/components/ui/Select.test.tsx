import { useState } from 'react'
import { cleanup, render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { afterEach, beforeAll, describe, expect, it, vi } from 'vitest'
import { Select } from './Select'

const CITIES = [
  'Blumenau',
  'Brusque',
  'Cascavel',
  'Concórdia',
  'Curitiba',
  'Joaçaba',
  'Joinville',
  'Lages',
  'Pato Branco',
]

beforeAll(() => {
  globalThis.ResizeObserver = class {
    observe() {}
    unobserve() {}
    disconnect() {}
  }
  Element.prototype.scrollIntoView = vi.fn()
})

afterEach(cleanup)

function ControlledSelect({
  cities,
  onChange,
}: {
  cities: string[]
  onChange?: (v: string) => void
}) {
  const [value, setValue] = useState('')
  return (
    <Select
      value={value}
      onValueChange={(next) => {
        setValue(next)
        onChange?.(next)
      }}
    >
      <option value="">Selecione uma cidade…</option>
      {cities.map((city) => (
        <option key={city} value={city}>
          {city}
        </option>
      ))}
    </Select>
  )
}

describe('Select', () => {
  it('mostra o rótulo da opção vazia como placeholder', () => {
    render(<ControlledSelect cities={CITIES.slice(0, 3)} />)
    expect(screen.getByRole('combobox')).toHaveTextContent('Selecione uma cidade…')
  })

  it('seleciona uma opção e mostra o rótulo escolhido', async () => {
    const onChange = vi.fn()
    render(<ControlledSelect cities={CITIES.slice(0, 3)} onChange={onChange} />)

    await userEvent.click(screen.getByRole('combobox'))
    await userEvent.click(await screen.findByText('Brusque'))

    expect(onChange).toHaveBeenCalledWith('Brusque')
    expect(screen.getByRole('combobox')).toHaveTextContent('Brusque')
  })

  it('não mostra busca em listas curtas', async () => {
    render(<ControlledSelect cities={CITIES.slice(0, 3)} />)
    await userEvent.click(screen.getByRole('combobox'))
    await screen.findByText('Blumenau')
    expect(screen.queryByPlaceholderText('Buscar…')).not.toBeInTheDocument()
  })

  it('filtra listas longas ignorando acentos', async () => {
    render(<ControlledSelect cities={CITIES} />)
    await userEvent.click(screen.getByRole('combobox'))
    await userEvent.type(await screen.findByPlaceholderText('Buscar…'), 'concordia')

    expect(screen.getByText('Concórdia')).toBeInTheDocument()
    expect(screen.queryByText('Lages')).not.toBeInTheDocument()
  })

  it('avisa quando a busca não encontra nada', async () => {
    render(<ControlledSelect cities={CITIES} />)
    await userEvent.click(screen.getByRole('combobox'))
    await userEvent.type(await screen.findByPlaceholderText('Buscar…'), 'zzz')

    expect(await screen.findByText('Nenhum resultado')).toBeInTheDocument()
  })
})
