import { Building2, Church, Landmark, LogIn, MapPin, RefreshCw, Search } from 'lucide-react'
import { Link } from 'react-router-dom'
import { cn } from '@/lib/utils'
import {
  collectionPointTypeLabels,
  formatAddress,
  type CollectionPointType,
} from '@/models/types'
import { useCollectionPointsMapViewModel } from '@/viewmodels'
import { AppLogo } from '@/views/components/common/AppLogo'
import { CollectionPointsMap } from '@/views/components/map/CollectionPointsMap'
import {
  Alert,
  Badge,
  Button,
  CenteredSpinner,
  EmptyState,
  Input,
  Select,
} from '@/views/components/ui'

const typeIcon: Record<CollectionPointType, typeof Building2> = {
  Association: Building2,
  Church,
  PublicEntity: Landmark,
}

export function MapPage() {
  const vm = useCollectionPointsMapViewModel()

  return (
    <div className="flex h-dvh flex-col">
      {/* Cabeçalho */}
      <header className="z-10 flex items-center justify-between border-b border-slate-200 bg-white px-4 py-3 shadow-sm sm:px-6">
        <AppLogo />
        <Link
          to="/login"
          className="inline-flex h-9 items-center gap-2 rounded-lg bg-brand-600 px-4 text-sm font-medium text-white shadow-sm transition-colors hover:bg-brand-700"
        >
          <LogIn className="size-4" />
          Entrar
        </Link>
      </header>

      {/* Corpo */}
      <div className="flex min-h-0 flex-1 flex-col lg:flex-row">
        {/* Painel lateral */}
        <aside className="flex w-full shrink-0 flex-col border-b border-slate-200 bg-white lg:w-95 lg:border-b-0 lg:border-r">
          <div className="space-y-3 border-b border-slate-100 p-4">
            <div>
              <h1 className="text-lg font-bold text-slate-900">Pontos de Coleta</h1>
              <p className="text-sm text-slate-500">
                Encontre onde doar ou retirar no mapa do OpenStreetMap.
              </p>
            </div>

            <div className="relative">
              <Search className="pointer-events-none absolute left-3 top-1/2 size-4 -translate-y-1/2 text-slate-400" />
              <Input
                value={vm.filters.search}
                onChange={(e) => vm.setFilters({ ...vm.filters, search: e.target.value })}
                placeholder="Buscar por nome, cidade ou bairro"
                className="pl-9"
              />
            </div>

            <div className="grid grid-cols-2 gap-2">
              <Select
                value={vm.filters.type}
                onChange={(e) =>
                  vm.setFilters({ ...vm.filters, type: e.target.value as typeof vm.filters.type })
                }
              >
                <option value="all">Todos os tipos</option>
                {Object.entries(collectionPointTypeLabels).map(([value, label]) => (
                  <option key={value} value={value}>
                    {label}
                  </option>
                ))}
              </Select>
              <Select
                value={vm.filters.city}
                onChange={(e) => vm.setFilters({ ...vm.filters, city: e.target.value })}
              >
                <option value="all">Todas as cidades</option>
                {vm.cities.map((city) => (
                  <option key={city} value={city}>
                    {city}
                  </option>
                ))}
              </Select>
            </div>

            <div className="flex items-center justify-between text-xs text-slate-500">
              <span>
                {vm.filtered.length} de {vm.total} ponto(s)
              </span>
              <span>
                {vm.geocodingPending ? 'Localizando endereços…' : `${vm.located} no mapa`}
              </span>
            </div>
          </div>

          {/* Lista */}
          <div className="min-h-0 flex-1 overflow-y-auto p-4 lg:max-h-none">
            {vm.isLoading ? (
              <CenteredSpinner label="Carregando pontos…" />
            ) : vm.isError ? (
              <Alert tone="error" title="Não foi possível carregar">
                {vm.error?.displayMessage}
                <Button
                  variant="secondary"
                  size="sm"
                  className="mt-2"
                  leftIcon={<RefreshCw className="size-4" />}
                  onClick={() => vm.refetch()}
                >
                  Tentar novamente
                </Button>
              </Alert>
            ) : vm.filtered.length === 0 ? (
              <EmptyState
                icon={<MapPin className="size-6" />}
                title="Nenhum ponto encontrado"
                description="Ajuste os filtros ou tente outra busca."
              />
            ) : (
              <ul className="space-y-2">
                {vm.filtered.map(({ item, position }) => {
                  const Icon = typeIcon[item.type]
                  const selected = item.id === vm.selectedId
                  return (
                    <li key={item.id}>
                      <button
                        type="button"
                        onClick={() => vm.setSelectedId(item.id)}
                        className={cn(
                          'w-full rounded-lg border p-3 text-left transition-colors',
                          selected
                            ? 'border-brand-400 bg-brand-50'
                            : 'border-slate-200 bg-white hover:border-slate-300',
                        )}
                      >
                        <div className="flex items-start justify-between gap-2">
                          <div className="flex items-start gap-2">
                            <span className="mt-0.5 grid size-8 shrink-0 place-items-center rounded-lg bg-slate-100 text-slate-500">
                              <Icon className="size-4" />
                            </span>
                            <div className="min-w-0">
                              <p className="truncate font-medium text-slate-900">{item.name}</p>
                              <p className="truncate text-xs text-slate-500">
                                {formatAddress(item.address)}
                              </p>
                            </div>
                          </div>
                          <Badge tone="brand">{collectionPointTypeLabels[item.type]}</Badge>
                        </div>
                        {!position && (
                          <p className="mt-2 text-xs text-amber-600">
                            {vm.geocodingPending ? 'Localizando…' : 'Endereço não localizado no mapa'}
                          </p>
                        )}
                      </button>
                    </li>
                  )
                })}
              </ul>
            )}
          </div>
        </aside>

        {/* Mapa */}
        <div className="relative min-h-90 flex-1">
          <CollectionPointsMap
            points={vm.filtered}
            selectedId={vm.selectedId}
            onSelect={vm.setSelectedId}
          />
        </div>
      </div>
    </div>
  )
}
