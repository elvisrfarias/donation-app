import { useAuth } from '@/viewmodels'
import { DashboardHeader } from '@/views/components/dashboard/DashboardHeader'
import { NetworkDashboard } from '@/views/components/dashboard/NetworkDashboard'
import { PointDashboard } from '@/views/components/dashboard/PointDashboard'
import { Alert } from '@/views/components/ui'

function firstName(fullName: string | undefined): string {
  return fullName?.split(' ')[0] ?? 'bem-vindo'
}

export function DashboardPage() {
  const { user, isAdmin, isManager } = useAuth()
  const greeting = `Olá, ${firstName(user?.fullName)}`

  if (isAdmin) {
    return (
      <div className="mx-auto w-[90%] space-y-5">
        <DashboardHeader title={greeting} subtitle="Visão geral da rede de pontos de coleta." />
        <NetworkDashboard />
      </div>
    )
  }

  return (
    <div className="mx-auto w-[90%]">
      {isManager ? (
        <PointDashboard pointId={user?.collectionPointId ?? null} title={greeting} />
      ) : (
        <div className="space-y-5">
          <DashboardHeader title={greeting} subtitle="Painel de indicadores." />
          <Alert tone="info">Seu perfil não tem indicadores para exibir neste painel.</Alert>
        </div>
      )}
    </div>
  )
}
