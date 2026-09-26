import { useState } from 'react'
import { cleanup, render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { afterEach, beforeAll, describe, expect, it, vi } from 'vitest'
import { dateToIso } from '@/lib/dateInput'
import { DatePicker } from './DatePicker'

beforeAll(() => {
  globalThis.ResizeObserver = class {
    observe() {}
    unobserve() {}
    disconnect() {}
  }
  Element.prototype.scrollIntoView = vi.fn()
})

afterEach(cleanup)

function ControlledDatePicker({
  initialValue = '',
  onChange,
}: {
  initialValue?: string
  onChange?: (value: string) => void
}) {
  const [value, setValue] = useState(initialValue)
  return (
    <DatePicker
      value={value}
      onValueChange={(next) => {
        setValue(next)
        onChange?.(next)
      }}
    />
  )
}

describe('DatePicker', () => {
  it('mostra a data recebida no formato brasileiro', () => {
    render(<ControlledDatePicker initialValue="2000-03-12" />)

    expect(screen.getByPlaceholderText('dd/mm/aaaa')).toHaveValue('12/03/2000')
  })

  it('aplica a máscara e devolve a data em ISO ao completar a digitação', async () => {
    const onChange = vi.fn()
    render(<ControlledDatePicker onChange={onChange} />)

    await userEvent.type(screen.getByPlaceholderText('dd/mm/aaaa'), '12032000')

    expect(screen.getByPlaceholderText('dd/mm/aaaa')).toHaveValue('12/03/2000')
    expect(onChange).toHaveBeenLastCalledWith('2000-03-12')
  })

  it('não aceita datas que não existem no calendário', async () => {
    const onChange = vi.fn()
    render(<ControlledDatePicker onChange={onChange} />)

    await userEvent.type(screen.getByPlaceholderText('dd/mm/aaaa'), '31022024')

    expect(onChange).not.toHaveBeenCalled()
    expect(screen.getByPlaceholderText('dd/mm/aaaa')).toBeInvalid()
  })

  it('não aceita datas futuras por padrão', async () => {
    const onChange = vi.fn()
    render(<ControlledDatePicker onChange={onChange} />)
    const nextYear = new Date().getFullYear() + 1

    await userEvent.type(screen.getByPlaceholderText('dd/mm/aaaa'), `0101${nextYear}`)

    expect(onChange).not.toHaveBeenCalled()
    expect(screen.getByPlaceholderText('dd/mm/aaaa')).toBeInvalid()
  })

  it('restaura a última data válida quando o campo perde o foco com texto inválido', async () => {
    render(<ControlledDatePicker initialValue="2000-03-12" />)
    const input = screen.getByPlaceholderText('dd/mm/aaaa')

    await userEvent.clear(input)
    await userEvent.type(input, '99')
    await userEvent.tab()

    expect(input).toHaveValue('')
  })

  it('limpa a data pelo botão', async () => {
    const onChange = vi.fn()
    render(<ControlledDatePicker initialValue="2000-03-12" onChange={onChange} />)

    await userEvent.click(screen.getByRole('button', { name: 'Limpar data' }))

    expect(onChange).toHaveBeenLastCalledWith('')
    expect(screen.getByPlaceholderText('dd/mm/aaaa')).toHaveValue('')
  })

  it('seleciona a data de hoje pelo calendário', async () => {
    const onChange = vi.fn()
    render(<ControlledDatePicker onChange={onChange} />)

    await userEvent.click(screen.getByRole('button', { name: 'Abrir calendário' }))
    await userEvent.click(await screen.findByRole('button', { name: 'Hoje' }))

    expect(onChange).toHaveBeenLastCalledWith(dateToIso(new Date()))
  })
})
