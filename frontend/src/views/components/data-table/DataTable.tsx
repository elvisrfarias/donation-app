import { type ReactNode, useMemo, useState } from 'react'
import {
  type ColumnDef,
  type ColumnFiltersState,
  type RowData,
  type SortingState,
  type VisibilityState,
  flexRender,
  getCoreRowModel,
  getFilteredRowModel,
  getPaginationRowModel,
  getSortedRowModel,
  useReactTable,
} from '@tanstack/react-table'
import {
  ArrowDownIcon,
  ArrowUpIcon,
  ChevronLeftIcon,
  ChevronRightIcon,
  ChevronsLeftIcon,
  ChevronsRightIcon,
  ChevronsUpDownIcon,
  FilterIcon,
  SearchIcon,
  SlidersHorizontalIcon,
} from 'lucide-react'
import { Checkbox } from '@/components/ui/checkbox'
import { Input } from '@/components/ui/input'
import { Popover, PopoverContent, PopoverTrigger } from '@/components/ui/popover'
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select'
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from '@/components/ui/table'
import { cn } from '@/lib/utils'
import { Button } from '@/views/components/ui'

// Metadados por coluna usados pelo DataTable (rótulo amigável, alinhamento, etc.).
declare module '@tanstack/react-table' {
  // eslint-disable-next-line @typescript-eslint/no-unused-vars
  interface ColumnMeta<TData extends RowData, TValue> {
    label?: string
    filterPlaceholder?: string
    align?: 'left' | 'center' | 'right'
    className?: string
    headerClassName?: string
  }
}

/**
 * Botão de ação exibido na coluna de ações (uma por linha). O `icon` é qualquer
 * nó — normalmente um ícone do lucide (`<Pencil className="size-4" />`). Use
 * `tone: 'danger'` para ações destrutivas (ex.: remover) e `hidden`/`disabled`
 * para controlar a visibilidade por linha.
 */
export interface RowAction<TData> {
  icon: ReactNode
  /** Rótulo acessível, usado no `aria-label` e no tooltip nativo. */
  label: string
  onClick: (row: TData) => void
  tone?: 'default' | 'danger'
  hidden?: (row: TData) => boolean
  disabled?: (row: TData) => boolean
}

interface DataTableProps<TData, TValue> {
  columns: ColumnDef<TData, TValue>[]
  data: TData[]
  /** Caixa de busca global (filtra todas as colunas). */
  searchable?: boolean
  searchPlaceholder?: string
  /** Habilita o botão que mostra os filtros por coluna. */
  filterable?: boolean
  pageSize?: number
  pageSizeOptions?: number[]
  emptyMessage?: ReactNode
  isLoading?: boolean
  /** Ações extras à direita da toolbar. */
  toolbar?: ReactNode
  /**
   * Botões de ação por linha (editar, remover, etc.). Quando informado, uma
   * coluna fixa de ações é anexada automaticamente à direita da tabela.
   */
  actions?: RowAction<TData>[]
  /** Cabeçalho da coluna de ações. */
  actionsLabel?: string
  className?: string
  /**
   * Altura máxima da área rolável da tabela (o cabeçalho fica fixo). Use `calc`
   * relativo ao viewport para nunca passar da tela — o valor desconta a toolbar,
   * a paginação e o conteúdo acima da tabela. Ajuste por página quando houver
   * mais (ou menos) coisa acima da tabela.
   */
  maxHeight?: string
}

const alignClass = { left: 'text-left', center: 'text-center', right: 'text-right' } as const

/** Botões de ação renderizados na coluna fixa de ações de cada linha. */
function RowActions<TData>({ row, actions }: { row: TData; actions: RowAction<TData>[] }) {
  const visible = actions.filter((action) => !action.hidden?.(row))
  if (visible.length === 0) return null

  return (
    <div className="flex items-center justify-end gap-0.5">
      {visible.map((action) => (
        <button
          key={action.label}
          type="button"
          title={action.label}
          aria-label={action.label}
          disabled={action.disabled?.(row)}
          onClick={() => action.onClick(row)}
          className={cn(
            'inline-flex size-8 items-center justify-center rounded-lg text-stone-500 transition-colors disabled:cursor-not-allowed disabled:opacity-40',
            action.tone === 'danger'
              ? 'hover:bg-red-50 hover:text-red-600'
              : 'hover:bg-stone-100 hover:text-stone-900',
          )}
        >
          {action.icon}
        </button>
      ))}
    </div>
  )
}

export function DataTable<TData, TValue>({
  columns,
  data,
  searchable = true,
  searchPlaceholder = 'Buscar…',
  filterable = true,
  pageSize = 10,
  pageSizeOptions = [10, 20, 30, 50],
  emptyMessage = 'Nenhum registro encontrado.',
  isLoading = false,
  toolbar,
  actions,
  actionsLabel = 'Ações',
  className,
  maxHeight = 'calc(100dvh - 18rem)',
}: DataTableProps<TData, TValue>) {
  const [sorting, setSorting] = useState<SortingState>([])
  const [columnFilters, setColumnFilters] = useState<ColumnFiltersState>([])
  const [globalFilter, setGlobalFilter] = useState('')
  const [columnVisibility, setColumnVisibility] = useState<VisibilityState>({})
  const [showFilters, setShowFilters] = useState(false)

  // Anexa a coluna fixa de ações (à direita) quando `actions` é informado. Ela
  // não é ordenável, filtrável nem ocultável — é só uma coluna de exibição.
  const tableColumns = useMemo<ColumnDef<TData, TValue>[]>(() => {
    if (!actions || actions.length === 0) return columns
    const actionColumn: ColumnDef<TData, TValue> = {
      id: '__actions',
      header: actionsLabel,
      enableSorting: false,
      enableColumnFilter: false,
      enableHiding: false,
      meta: { label: actionsLabel, align: 'right', className: 'w-0 whitespace-nowrap' },
      cell: ({ row }) => <RowActions row={row.original} actions={actions} />,
    }
    return [...columns, actionColumn]
  }, [columns, actions, actionsLabel])

  // useReactTable retorna funções não-memoizáveis; o React Compiler pula este componente
  // (limitação conhecida do TanStack Table, sem efeito funcional).
  // eslint-disable-next-line react-hooks/incompatible-library
  const table = useReactTable({
    data,
    columns: tableColumns,
    state: { sorting, columnFilters, globalFilter, columnVisibility },
    globalFilterFn: 'includesString',
    onSortingChange: setSorting,
    onColumnFiltersChange: setColumnFilters,
    onGlobalFilterChange: setGlobalFilter,
    onColumnVisibilityChange: setColumnVisibility,
    getCoreRowModel: getCoreRowModel(),
    getSortedRowModel: getSortedRowModel(),
    getFilteredRowModel: getFilteredRowModel(),
    getPaginationRowModel: getPaginationRowModel(),
    initialState: { pagination: { pageSize } },
  })

  const hideableColumns = table.getAllColumns().filter((c) => c.getCanHide())
  const hasColumnFilters = table.getAllColumns().some((c) => c.getCanFilter())
  const totalRows = table.getFilteredRowModel().rows.length
  const { pageIndex, pageSize: currentPageSize } = table.getState().pagination
  const firstRow = totalRows === 0 ? 0 : pageIndex * currentPageSize + 1
  const lastRow = Math.min((pageIndex + 1) * currentPageSize, totalRows)
  const visibleColumnCount = table.getVisibleLeafColumns().length

  return (
    <div className={cn('space-y-3', className)}>
      {/* Toolbar */}
      <div className="flex flex-wrap items-center gap-2">
        {searchable && (
          <div className="relative max-w-xs flex-1">
            <SearchIcon className="absolute top-1/2 left-3 size-4 -translate-y-1/2 text-stone-400" />
            <Input
              value={globalFilter}
              onChange={(e) => setGlobalFilter(e.target.value)}
              placeholder={searchPlaceholder}
              className="h-10 pl-9"
            />
          </div>
        )}

        <div className="ml-auto flex items-center gap-2">
          {toolbar}

          {filterable && hasColumnFilters && (
            <Button
              variant={showFilters ? 'brand' : 'secondary'}
              size="sm"
              leftIcon={<FilterIcon className="size-4" />}
              onClick={() => setShowFilters((v) => !v)}
            >
              Filtros
            </Button>
          )}

          {hideableColumns.length > 0 && (
            <Popover>
              <PopoverTrigger asChild>
                <Button variant="secondary" size="sm" leftIcon={<SlidersHorizontalIcon className="size-4" />}>
                  Colunas
                </Button>
              </PopoverTrigger>
              <PopoverContent align="end" className="w-52 p-1.5">
                <p className="px-2 py-1.5 text-xs font-medium text-stone-500">Exibir colunas</p>
                {hideableColumns.map((column) => (
                  <label
                    key={column.id}
                    className="flex cursor-pointer items-center gap-2.5 rounded-lg px-2 py-1.5 text-sm text-stone-700 hover:bg-accent"
                  >
                    <Checkbox
                      checked={column.getIsVisible()}
                      onCheckedChange={(value) => column.toggleVisibility(!!value)}
                    />
                    {column.columnDef.meta?.label ?? column.id}
                  </label>
                ))}
              </PopoverContent>
            </Popover>
          )}
        </div>
      </div>

      {/* Tabela — área rolável limitada por calc(viewport); cabeçalho fixo. */}
      <div
        className="overflow-hidden rounded-2xl border border-stone-200 bg-white shadow-card"
        style={{ ['--dt-max-h' as string]: maxHeight }}
      >
        <Table containerClassName="max-h-[var(--dt-max-h)] overflow-y-auto">
          <TableHeader>
            {table.getHeaderGroups().map((headerGroup) => (
              <TableRow key={headerGroup.id} className="bg-stone-50/60 hover:bg-stone-50/60">
                {headerGroup.headers.map((header) => {
                  const meta = header.column.columnDef.meta
                  const canSort = header.column.getCanSort()
                  const sorted = header.column.getIsSorted()
                  const headerContent = header.isPlaceholder
                    ? null
                    : flexRender(header.column.columnDef.header, header.getContext())

                  return (
                    <TableHead
                      key={header.id}
                      className={cn(
                        'sticky top-0 z-10 bg-stone-50',
                        meta?.align && alignClass[meta.align],
                        meta?.headerClassName,
                      )}
                    >
                      {canSort ? (
                        <button
                          type="button"
                          onClick={header.column.getToggleSortingHandler()}
                          className="-ml-1 inline-flex items-center gap-1.5 rounded-md px-1 py-0.5 transition-colors hover:text-stone-900"
                        >
                          {headerContent}
                          {sorted === 'asc' ? (
                            <ArrowUpIcon className="size-3.5" />
                          ) : sorted === 'desc' ? (
                            <ArrowDownIcon className="size-3.5" />
                          ) : (
                            <ChevronsUpDownIcon className="size-3.5 text-stone-400" />
                          )}
                        </button>
                      ) : (
                        headerContent
                      )}

                      {showFilters && header.column.getCanFilter() && (
                        <Input
                          value={(header.column.getFilterValue() as string) ?? ''}
                          onChange={(e) => header.column.setFilterValue(e.target.value)}
                          placeholder={meta?.filterPlaceholder ?? 'Filtrar…'}
                          className="mt-2 h-8 text-xs font-normal"
                        />
                      )}
                    </TableHead>
                  )
                })}
              </TableRow>
            ))}
          </TableHeader>

          <TableBody>
            {isLoading ? (
              <TableRow>
                <TableCell colSpan={visibleColumnCount} className="h-24 text-center text-stone-500">
                  Carregando…
                </TableCell>
              </TableRow>
            ) : table.getRowModel().rows.length === 0 ? (
              <TableRow>
                <TableCell colSpan={visibleColumnCount} className="h-24 text-center text-stone-500">
                  {emptyMessage}
                </TableCell>
              </TableRow>
            ) : (
              table.getRowModel().rows.map((row) => (
                <TableRow key={row.id} data-state={row.getIsSelected() ? 'selected' : undefined}>
                  {row.getVisibleCells().map((cell) => {
                    const meta = cell.column.columnDef.meta
                    return (
                      <TableCell
                        key={cell.id}
                        className={cn(meta?.align && alignClass[meta.align], meta?.className)}
                      >
                        {flexRender(cell.column.columnDef.cell, cell.getContext())}
                      </TableCell>
                    )
                  })}
                </TableRow>
              ))
            )}
          </TableBody>
        </Table>
      </div>

      {/* Paginação */}
      <div className="flex flex-col-reverse items-center justify-between gap-3 sm:flex-row">
        <p className="text-xs text-stone-500">
          {totalRows === 0
            ? 'Nenhum resultado'
            : `Mostrando ${firstRow}–${lastRow} de ${totalRows}`}
        </p>

        <div className="flex items-center gap-4">
          <div className="flex items-center gap-2">
            <span className="text-xs text-stone-500">Por página</span>
            <Select
              value={String(currentPageSize)}
              onValueChange={(value) => table.setPageSize(Number(value))}
            >
              <SelectTrigger className="h-9 w-18">
                <SelectValue />
              </SelectTrigger>
              <SelectContent>
                {pageSizeOptions.map((size) => (
                  <SelectItem key={size} value={String(size)}>
                    {size}
                  </SelectItem>
                ))}
              </SelectContent>
            </Select>
          </div>

          <span className="text-xs text-stone-500">
            Página {table.getState().pagination.pageIndex + 1} de {Math.max(table.getPageCount(), 1)}
          </span>

          <div className="flex items-center gap-1">
            <Button
              variant="secondary"
              size="sm"
              className="px-2"
              aria-label="Primeira página"
              onClick={() => table.setPageIndex(0)}
              disabled={!table.getCanPreviousPage()}
            >
              <ChevronsLeftIcon className="size-4" />
            </Button>
            <Button
              variant="secondary"
              size="sm"
              className="px-2"
              aria-label="Página anterior"
              onClick={() => table.previousPage()}
              disabled={!table.getCanPreviousPage()}
            >
              <ChevronLeftIcon className="size-4" />
            </Button>
            <Button
              variant="secondary"
              size="sm"
              className="px-2"
              aria-label="Próxima página"
              onClick={() => table.nextPage()}
              disabled={!table.getCanNextPage()}
            >
              <ChevronRightIcon className="size-4" />
            </Button>
            <Button
              variant="secondary"
              size="sm"
              className="px-2"
              aria-label="Última página"
              onClick={() => table.setPageIndex(table.getPageCount() - 1)}
              disabled={!table.getCanNextPage()}
            >
              <ChevronsRightIcon className="size-4" />
            </Button>
          </div>
        </div>
      </div>
    </div>
  )
}
