import { cleanup, render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { useState } from 'react'
import { afterEach, describe, expect, it } from 'vitest'
import { TagBox } from './TagBox'

const options = [
  { value: 'Food', label: 'Cestas básicas' },
  { value: 'Clothing', label: 'Roupas' },
  { value: 'Footwear', label: 'Calçados' },
]

afterEach(cleanup)

function Harness({ initial = ['Food'] }: { initial?: string[] }) {
  const [value, setValue] = useState(initial)
  return <TagBox id="items" options={options} value={value} onValueChange={setValue} />
}

describe('TagBox', () => {
  it('mostra os itens selecionados como tags', () => {
    render(<Harness initial={['Food', 'Clothing']} />)

    expect(screen.getByText('Cestas básicas')).toBeInTheDocument()
    expect(screen.getByText('Roupas')).toBeInTheDocument()
    expect(screen.queryByText('Calçados')).not.toBeInTheDocument()
  })

  it('adiciona mais de um item pela lista, mantendo a ordem das opções', async () => {
    render(<Harness initial={['Footwear']} />)

    await userEvent.click(screen.getByRole('combobox'))
    await userEvent.click(await screen.findByRole('option', { name: 'Cestas básicas' }))
    await userEvent.click(await screen.findByRole('option', { name: 'Roupas' }))

    const tags = screen
      .getAllByRole('button', { name: /^Remover/ })
      .map((b) => b.getAttribute('aria-label'))
    expect(tags).toEqual(['Remover Cestas básicas', 'Remover Roupas', 'Remover Calçados'])
  })

  it('remove uma tag pelo X sem abrir a lista', async () => {
    render(<Harness initial={['Food', 'Clothing']} />)

    await userEvent.click(screen.getByRole('button', { name: 'Remover Roupas' }))

    expect(screen.queryByText('Roupas')).not.toBeInTheDocument()
    expect(screen.getByText('Cestas básicas')).toBeInTheDocument()
    expect(screen.queryByRole('option')).not.toBeInTheDocument()
  })

  it('desmarca um item já selecionado ao clicar nele na lista', async () => {
    render(<Harness initial={['Food']} />)

    await userEvent.click(screen.getByRole('combobox'))
    await userEvent.click(await screen.findByRole('option', { name: 'Cestas básicas' }))

    expect(screen.queryByRole('button', { name: 'Remover Cestas básicas' })).not.toBeInTheDocument()
  })

  it('mostra o texto de ajuda quando nada está selecionado', () => {
    render(<Harness initial={[]} />)

    expect(screen.getByText('Selecione…')).toBeInTheDocument()
  })
})
