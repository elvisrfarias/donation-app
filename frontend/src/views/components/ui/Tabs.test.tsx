import { useState } from 'react'
import { cleanup, render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { afterEach, describe, expect, it } from 'vitest'
import { TabPanel, Tabs } from './Tabs'

afterEach(cleanup)

function Example() {
  const [value, setValue] = useState('a')
  return (
    <>
      <Tabs
        ariaLabel="Seções"
        value={value}
        onValueChange={setValue}
        tabs={[
          { value: 'a', label: 'Primeira' },
          { value: 'b', label: 'Segunda', badge: 3 },
        ]}
      />
      <TabPanel value="a" activeValue={value}>
        <input aria-label="rascunho" />
      </TabPanel>
      <TabPanel value="b" activeValue={value}>
        Conteúdo B
      </TabPanel>
    </>
  )
}

describe('Tabs', () => {
  it('mostra só o painel da aba ativa e marca a aba como selecionada', async () => {
    render(<Example />)

    expect(screen.getByRole('tab', { name: 'Primeira' })).toHaveAttribute('aria-selected', 'true')
    expect(screen.getByText('Conteúdo B')).not.toBeVisible()

    await userEvent.click(screen.getByRole('tab', { name: /Segunda/ }))

    expect(screen.getByText('Conteúdo B')).toBeVisible()
    expect(screen.getByRole('tab', { name: /Segunda/ })).toHaveAttribute('aria-selected', 'true')
  })

  it('mantém o painel oculto montado, preservando o que foi digitado', async () => {
    render(<Example />)
    await userEvent.type(screen.getByLabelText('rascunho'), 'abc')

    await userEvent.click(screen.getByRole('tab', { name: /Segunda/ }))
    await userEvent.click(screen.getByRole('tab', { name: 'Primeira' }))

    expect(screen.getByLabelText('rascunho')).toHaveValue('abc')
  })

  it('navega entre as abas com as setas do teclado', async () => {
    render(<Example />)

    screen.getByRole('tab', { name: 'Primeira' }).focus()
    await userEvent.keyboard('{ArrowRight}')

    expect(screen.getByRole('tab', { name: /Segunda/ })).toHaveAttribute('aria-selected', 'true')
    expect(screen.getByRole('tab', { name: /Segunda/ })).toHaveFocus()
  })

  it('mostra o contador de pendências na aba', () => {
    render(<Example />)

    expect(screen.getByRole('tab', { name: /Segunda/ })).toHaveTextContent('3')
  })
})
