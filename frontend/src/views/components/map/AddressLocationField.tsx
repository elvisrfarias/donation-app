import { useEffect, useRef, useState } from 'react'
import { type AnyFormApi, useStore } from '@tanstack/react-form'
import { Check, MapPinned } from 'lucide-react'
import { cepService } from '@/models/services'
import { formatAddress, type LatLng } from '@/models/types'
import { Button } from '@/views/components/ui'
import { LocationPickerModal, type PickedLocation } from './LocationPickerModal'

interface AddressValues {
  street?: string
  number?: string
  district?: string
  city?: string
  state?: string
  zipCode?: string
  latitude?: number
  longitude?: number
}

function onlyDigits(value: string | undefined): string {
  return (value ?? '').replace(/\D/g, '')
}

function buildQuery(values: AddressValues): string | undefined {
  if (!values.city && !values.zipCode) return undefined
  return formatAddress({
    street: values.street ?? '',
    number: values.number ?? '',
    complement: null,
    district: values.district ?? '',
    city: values.city ?? '',
    state: values.state ?? '',
    zipCode: values.zipCode ?? '',
  })
}

/**
 * Ferramentas de localização do endereço no formulário de ponto de coleta:
 * autopreenche os campos a partir do CEP (BrasilAPI) e abre um seletor de mapa
 * para fixar o ponto exato, capturando as coordenadas. As coordenadas só ficam
 * definidas quando o usuário escolhe no mapa; alterar o CEP as zera para forçar
 * uma nova geocodificação no envio.
 */
export function AddressLocationField({ form }: { form: AnyFormApi }) {
  const values = useStore(form.store, (s) => s.values as AddressValues)
  const [pickerOpen, setPickerOpen] = useState(false)
  const lastCepRef = useRef<string>(onlyDigits((form.state.values as AddressValues).zipCode))

  const hasCoordinates = values.latitude != null && values.longitude != null

  useEffect(() => {
    const cep = onlyDigits(values.zipCode)
    if (cep.length !== 8 || cep === lastCepRef.current) return undefined
    lastCepRef.current = cep

    let cancelled = false
    cepService.lookup(cep).then((result) => {
      if (cancelled || !result) return
      if (result.street) form.setFieldValue('street', result.street)
      if (result.district) form.setFieldValue('district', result.district)
      if (result.city) form.setFieldValue('city', result.city)
      if (result.state) form.setFieldValue('state', result.state)
      form.setFieldValue('latitude', undefined)
      form.setFieldValue('longitude', undefined)
    })
    return () => {
      cancelled = true
    }
  }, [values.zipCode, form])

  function applyPicked({ position, address }: PickedLocation) {
    if (address.street) form.setFieldValue('street', address.street)
    if (address.number) form.setFieldValue('number', address.number)
    if (address.district) form.setFieldValue('district', address.district)
    if (address.city) form.setFieldValue('city', address.city)
    if (address.state) form.setFieldValue('state', address.state)
    if (address.zipCode) {
      lastCepRef.current = address.zipCode
      form.setFieldValue('zipCode', address.zipCode)
    }
    form.setFieldValue('latitude', position.lat)
    form.setFieldValue('longitude', position.lng)
  }

  const initialPosition: LatLng | null = hasCoordinates
    ? { lat: values.latitude as number, lng: values.longitude as number }
    : null

  return (
    <div className="flex flex-col gap-2 rounded-2xl border border-dashed border-stone-300 bg-stone-50/60 p-3 sm:flex-row sm:items-center sm:justify-between">
      <div className="flex items-center gap-2 text-sm">
        {hasCoordinates ? (
          <>
            <Check className="size-4 shrink-0 text-emerald-600" />
            <span className="text-stone-600">
              Local marcado no mapa
              <span className="ml-1 text-xs text-stone-400">
                ({(values.latitude as number).toFixed(5)}, {(values.longitude as number).toFixed(5)}
                )
              </span>
            </span>
          </>
        ) : (
          <span className="text-stone-500">
            Localização pelo endereço/CEP. Para precisão, marque no mapa.
          </span>
        )}
      </div>
      <Button
        type="button"
        variant="secondary"
        size="sm"
        leftIcon={<MapPinned className="size-4" />}
        onClick={() => setPickerOpen(true)}
      >
        Selecionar no mapa
      </Button>

      <LocationPickerModal
        open={pickerOpen}
        onOpenChange={setPickerOpen}
        initialPosition={initialPosition}
        initialQuery={buildQuery(values)}
        onConfirm={applyPicked}
      />
    </div>
  )
}
