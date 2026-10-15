import { useState } from 'react'
import { Megaphone, Send, Trash2 } from 'lucide-react'
import { formatDateTime } from '@/lib/utils'
import {
  MAX_NOTICE_MESSAGE_LENGTH,
  MAX_NOTICE_TITLE_LENGTH,
  noticeAudienceLabels,
  type Notice,
  type NoticeAudience,
} from '@/models/types'
import { useArchiveNotice, useAuth, useNetworkNotices, usePublishNotice } from '@/viewmodels'
import { HowItWorks } from '@/views/components/common/HowItWorks'
import {
  Alert,
  Badge,
  Button,
  Card,
  CenteredSpinner,
  EmptyState,
  FormField,
  Input,
  SectionTitle,
  Textarea,
} from '@/views/components/ui'

const audienceOptions: NoticeAudience[] = ['Public', 'Managers']

function PublishNoticeForm({ pointId }: { pointId: string }) {
  const publish = usePublishNotice(pointId)
  const [title, setTitle] = useState('')
  const [message, setMessage] = useState('')
  const [audience, setAudience] = useState<NoticeAudience>('Public')

  const canSubmit = title.trim() !== '' && message.trim() !== ''

  function handleSubmit() {
    publish.mutate(
      { title: title.trim(), message: message.trim(), audience },
      {
        onSuccess: () => {
          setTitle('')
          setMessage('')
        },
      },
    )
  }

  return (
    <Card className="p-6">
      <form
        className="space-y-4"
        onSubmit={(event) => {
          event.preventDefault()
          handleSubmit()
        }}
      >
        <h2 className="text-base font-semibold text-stone-900">Novo aviso</h2>
        {publish.error && <Alert tone="error">{publish.error.displayMessage}</Alert>}
        {publish.isSuccess && <Alert tone="success">Aviso publicado no mural.</Alert>}

        <FormField label="Título" htmlFor="notice-title" required>
          <Input
            id="notice-title"
            value={title}
            onChange={(event) => setTitle(event.target.value)}
            maxLength={MAX_NOTICE_TITLE_LENGTH}
          />
        </FormField>

        <FormField label="Mensagem" htmlFor="notice-message" required>
          <Textarea
            id="notice-message"
            value={message}
            onChange={(event) => setMessage(event.target.value)}
            maxLength={MAX_NOTICE_MESSAGE_LENGTH}
          />
        </FormField>

        <fieldset className="space-y-2">
          <legend className="text-sm font-semibold text-stone-700">Quem pode ver</legend>
          <div className="flex flex-wrap gap-4">
            {audienceOptions.map((option) => (
              <label
                key={option}
                className="flex min-h-11 items-center gap-2 text-sm text-stone-700"
              >
                <input
                  type="radio"
                  name="notice-audience"
                  value={option}
                  checked={audience === option}
                  onChange={() => setAudience(option)}
                  className="size-4"
                />
                {noticeAudienceLabels[option]}
              </label>
            ))}
          </div>
        </fieldset>

        <Button
          type="submit"
          variant="brand"
          leftIcon={<Send className="size-4" />}
          disabled={!canSubmit}
          loading={publish.isPending}
        >
          Publicar aviso
        </Button>
      </form>
    </Card>
  )
}

function NoticeItem({
  notice,
  canArchive,
  pointId,
}: {
  notice: Notice
  canArchive: boolean
  pointId: string
}) {
  const archive = useArchiveNotice(pointId)
  const [confirmingArchive, setConfirmingArchive] = useState(false)

  return (
    <li className="flex items-start gap-3 py-4">
      <div className="min-w-0 flex-1">
        <div className="flex flex-wrap items-center gap-2">
          <p className="font-semibold text-stone-900">{notice.title}</p>
          <Badge tone={notice.audience === 'Public' ? 'brand' : 'slate'}>
            {noticeAudienceLabels[notice.audience]}
          </Badge>
        </div>
        <p className="mt-1 text-sm whitespace-pre-line text-stone-600">{notice.message}</p>
        <p className="mt-1.5 text-xs text-stone-500">
          {notice.pointName ?? notice.authorName} · {formatDateTime(notice.createdAtUtc)}
        </p>
      </div>
      {canArchive &&
        (confirmingArchive ? (
          <div className="flex shrink-0 gap-2">
            <Button
              size="sm"
              variant="danger"
              loading={archive.isPending}
              onClick={() => archive.mutate(notice.id)}
            >
              Retirar do mural
            </Button>
            <Button size="sm" variant="ghost" onClick={() => setConfirmingArchive(false)}>
              Voltar
            </Button>
          </div>
        ) : (
          <Button
            variant="ghost"
            size="sm"
            aria-label={`Retirar o aviso ${notice.title}`}
            onClick={() => setConfirmingArchive(true)}
          >
            <Trash2 className="size-4" aria-hidden />
          </Button>
        ))}
    </li>
  )
}

function NetworkNotices({ canManage }: { canManage: (notice: Notice) => boolean }) {
  const { notices, isLoading, isError, error } = useNetworkNotices()

  if (isLoading) return <CenteredSpinner label="Carregando mural…" />
  if (isError) return <Alert tone="error">{error?.displayMessage}</Alert>
  if (notices.length === 0) {
    return (
      <EmptyState
        icon={<Megaphone className="size-6" />}
        title="Nenhum aviso no mural"
        description="Os avisos publicados pelos pontos da rede aparecem aqui."
      />
    )
  }

  return (
    <Card className="px-6">
      <ul className="divide-y divide-stone-100" aria-live="polite">
        {notices.map((notice) => (
          <NoticeItem
            key={notice.id}
            notice={notice}
            pointId={notice.collectionPointId}
            canArchive={canManage(notice)}
          />
        ))}
      </ul>
    </Card>
  )
}

export function NoticesPage() {
  const { user, isAdmin } = useAuth()
  const ownPointId = user?.collectionPointId ?? null

  return (
    <div className="mx-auto w-[90%] space-y-6">
      <SectionTitle
        icon={<Megaphone className="size-5" />}
        title="Mural de avisos"
        description="Comunique necessidades à comunidade e combine ações com os outros pontos da rede."
      />

      <HowItWorks>
        Avisos para a <strong>comunidade</strong> aparecem no mapa público, na página do seu ponto
        (bom para pedir itens em falta). Avisos <strong>só para gestores</strong> circulam apenas
        entre os pontos da rede. Você só retira os avisos do seu próprio ponto.
      </HowItWorks>

      {ownPointId ? (
        <PublishNoticeForm pointId={ownPointId} />
      ) : (
        isAdmin && (
          <Alert tone="info">
            Administradores acompanham o mural da rede. Para publicar, entre como gestor de um
            ponto.
          </Alert>
        )
      )}

      <NetworkNotices canManage={(notice) => isAdmin || notice.collectionPointId === ownPointId} />
    </div>
  )
}
