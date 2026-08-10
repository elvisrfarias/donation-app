import { render, screen } from '@testing-library/react'
import { describe, expect, it, vi } from 'vitest'
import { ErrorBoundary } from './ErrorBoundary'

function Bomb(): never {
  throw new Error('estouro de teste')
}

describe('ErrorBoundary', () => {
  it('renderiza os filhos normalmente quando não há erro', () => {
    render(
      <ErrorBoundary>
        <p>conteúdo normal</p>
      </ErrorBoundary>,
    )

    expect(screen.getByText('conteúdo normal')).toBeInTheDocument()
  })

  it('captura o erro de um filho e mostra a tela de fallback em vez de derrubar a árvore', () => {
    vi.spyOn(console, 'error').mockImplementation(() => {})

    render(
      <ErrorBoundary>
        <Bomb />
      </ErrorBoundary>,
    )

    expect(screen.getByText('Algo deu errado')).toBeInTheDocument()
    expect(screen.getByText('estouro de teste')).toBeInTheDocument()
    expect(screen.getByRole('button', { name: /recarregar/i })).toBeInTheDocument()

    vi.restoreAllMocks()
  })
})
