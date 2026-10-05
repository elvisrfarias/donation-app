import { type ReactNode, useEffect, useRef, useState } from 'react'
import { cn } from '@/lib/utils'

const REVEAL_THRESHOLD = 0.15

export function Reveal({
  children,
  delayMilliseconds = 0,
  className,
}: {
  children: ReactNode
  delayMilliseconds?: number
  className?: string
}) {
  const ref = useRef<HTMLDivElement>(null)
  const [visible, setVisible] = useState(false)

  useEffect(() => {
    const element = ref.current
    if (!element) return
    const observer = new IntersectionObserver(
      ([entry]) => {
        if (!entry.isIntersecting) return
        setVisible(true)
        observer.disconnect()
      },
      { threshold: REVEAL_THRESHOLD },
    )
    observer.observe(element)
    return () => observer.disconnect()
  }, [])

  return (
    <div
      ref={ref}
      style={{ transitionDelay: `${delayMilliseconds}ms` }}
      className={cn(
        'transition-all duration-700 ease-out',
        visible ? 'translate-y-0 opacity-100' : 'translate-y-8 opacity-0',
        className,
      )}
    >
      {children}
    </div>
  )
}
