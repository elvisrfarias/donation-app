import { useState } from 'react'
import { Boxes, RefreshCw, SlidersHorizontal } from 'lucide-react'
import { useCollectionPoint } from '@/viewmodels'
import { Alert, Button, CenteredSpinner, TabPanel, Tabs, type TabItem } from '@/views/components/ui'
import { StockEditor } from './StockEditor'
import { categoriesNeedingRestock, toStockQuantities, toStockSettings } from './stockDraft'
import { StockSettingsEditor } from './StockSettingsEditor'

interface StockWorkspaceProps {
  pointId: string
  canEdit: boolean
}

const STOCK_TAB = 'estoque'
const SETTINGS_TAB = 'configuracao'

export function StockWorkspace({ pointId, canEdit }: StockWorkspaceProps) {
  const { point, isLoading, isError, error, refetch } = useCollectionPoint(pointId)
  const [activeTab, setActiveTab] = useState(STOCK_TAB)

  if (isLoading) return <CenteredSpinner label="Carregando estoque…" />

  if (isError || !point) {
    return (
      <Alert tone="error" title="Não foi possível carregar o estoque">
        {error?.displayMessage}
        <Button
          variant="secondary"
          size="sm"
          className="mt-2"
          leftIcon={<RefreshCw className="size-4" />}
          onClick={() => refetch()}
        >
          Tentar novamente
        </Button>
      </Alert>
    )
  }

  const restockCount = categoriesNeedingRestock(
    toStockQuantities(point),
    toStockSettings(point),
  ).length
  const tabs: TabItem[] = [
    { value: STOCK_TAB, label: 'Estoque', icon: <Boxes className="size-4" />, badge: restockCount },
    {
      value: SETTINGS_TAB,
      label: 'Metas e avisos',
      icon: <SlidersHorizontal className="size-4" />,
    },
  ]

  return (
    <div className="space-y-5">
      <Tabs
        tabs={tabs}
        value={activeTab}
        onValueChange={setActiveTab}
        ariaLabel="Seções do estoque"
      />

      <TabPanel value={STOCK_TAB} activeValue={activeTab}>
        <StockEditor
          key={point.id}
          point={point}
          canEdit={canEdit}
          onOpenSettings={() => setActiveTab(SETTINGS_TAB)}
        />
      </TabPanel>
      <TabPanel value={SETTINGS_TAB} activeValue={activeTab}>
        <StockSettingsEditor key={point.id} point={point} canEdit={canEdit} />
      </TabPanel>
    </div>
  )
}
