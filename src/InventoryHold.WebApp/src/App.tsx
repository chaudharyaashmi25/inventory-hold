import React, { useEffect, useState } from 'react'
import { fetchInventory, createHold, releaseHold, fetchActiveHolds } from './api'
import './App.css'

// TYPES
type InventoryItem = { sku: string; name: string; availableQuantity: number }
type Hold = { id: string; sku: string; quantity: number; expiresAt?: string }

type Toast = {
  id: string
  title: string
  desc: string
  type: 'success' | 'error' | 'warning'
}

// INLINE SVG ICONS
interface IconProps {
  className?: string
}

const PackageIcon = ({ className = 'icon-md' }: IconProps) => (
  <svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" strokeWidth={1.5} stroke="currentColor" className={`icon-svg ${className}`}>
    <path strokeLinecap="round" strokeLinejoin="round" d="M21 7.5l-9-5.25L3 7.5m18 0l-9 5.25m9-5.25v9l-9 5.25M3 7.5l9 5.25M3 7.5v9l9 5.25m0-9v9" />
  </svg>
)

const ClockIcon = ({ className = 'icon-md' }: IconProps) => (
  <svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" strokeWidth={1.5} stroke="currentColor" className={`icon-svg ${className}`}>
    <path strokeLinecap="round" strokeLinejoin="round" d="M12 6v6h4.5m4.5 0a9 9 0 11-18 0 9 9 0 0118 0z" />
  </svg>
)

const LayersIcon = ({ className = 'icon-md' }: IconProps) => (
  <svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" strokeWidth={1.5} stroke="currentColor" className={`icon-svg ${className}`}>
    <path strokeLinecap="round" strokeLinejoin="round" d="M6.429 9.75L2.25 12l4.179 2.25m11.142 0L21.75 12l-4.179-2.25m-11.142 0L10.5 7.5 12 6.75 13.5 7.5l4.179 2.25m-11.142 0L12 12.75l5.571-3m-11.142 4.5L12 16.5l5.571-3M12 16.5v4.5" />
  </svg>
)

const ShieldCheckIcon = ({ className = 'icon-md' }: IconProps) => (
  <svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" strokeWidth={1.5} stroke="currentColor" className={`icon-svg ${className}`}>
    <path strokeLinecap="round" strokeLinejoin="round" d="M9 12.75L11.25 15 15 9.75M21 12c0 1.268-.63 2.39-1.593 3.068a3.745 3.745 0 01-1.043 3.296 3.745 3.745 0 01-3.296 1.043A3.745 3.745 0 0112 21c-1.268 0-2.39-.63-3.068-1.593a3.746 3.746 0 01-3.296-1.043 3.745 3.745 0 01-1.043-3.296A3.745 3.745 0 013 12c0-1.268.63-2.39 1.593-3.068a3.745 3.745 0 011.043-3.296 3.746 3.746 0 013.296-1.043A3.746 3.746 0 0112 3c1.268 0 2.39.63 3.068 1.593a3.746 3.746 0 013.296 1.043 3.746 3.746 0 011.043 3.296A3.745 3.745 0 0121 12z" />
  </svg>
)

const SearchIcon = ({ className = 'icon-md' }: IconProps) => (
  <svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" strokeWidth={1.5} stroke="currentColor" className={`icon-svg search-icon ${className}`}>
    <path strokeLinecap="round" strokeLinejoin="round" d="M21 21l-5.197-5.197m0 0A7.5 7.5 0 105.196 5.196a7.5 7.5 0 0010.637 10.637z" />
  </svg>
)

const RefreshCwIcon = ({ className = 'icon-md' }: IconProps) => (
  <svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" strokeWidth={1.5} stroke="currentColor" className={`icon-svg ${className}`}>
    <path strokeLinecap="round" strokeLinejoin="round" d="M16.023 9.348h4.992v-.001M2.985 19.644v-4.992m0 0h4.992m-4.993 0l3.181 3.183a8.25 8.25 0 0013.803-3.7M4.031 9.865a8.25 8.25 0 0113.803-3.7l3.181 3.182m0-4.991v4.99" />
  </svg>
)

const XIcon = ({ className = 'icon-md' }: IconProps) => (
  <svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" strokeWidth={1.5} stroke="currentColor" className={`icon-svg ${className}`}>
    <path strokeLinecap="round" strokeLinejoin="round" d="M6 18L18 6M6 6l12 12" />
  </svg>
)

const ChevronDownIcon = ({ className = 'icon-md' }: IconProps) => (
  <svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" strokeWidth={1.5} stroke="currentColor" className={`icon-svg form-select-icon ${className}`}>
    <path strokeLinecap="round" strokeLinejoin="round" d="M19.5 8.25l-7.5 7.5-7.5-7.5" />
  </svg>
)

const LockIcon = ({ className = 'icon-md' }: IconProps) => (
  <svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" strokeWidth={1.5} stroke="currentColor" className={`icon-svg ${className}`}>
    <path strokeLinecap="round" strokeLinejoin="round" d="M16.5 10.5V6.75a4.5 4.5 0 10-9 0v3.75m-.75 11.25h10.5a2.25 2.25 0 002.25-2.25v-6.75a2.25 2.25 0 00-2.25-2.25H6.75a2.25 2.25 0 00-2.25 2.25v6.75a2.25 2.25 0 002.25 2.25z" />
  </svg>
)

const UnlockIcon = ({ className = 'icon-md' }: IconProps) => (
  <svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" strokeWidth={1.5} stroke="currentColor" className={`icon-svg ${className}`}>
    <path strokeLinecap="round" strokeLinejoin="round" d="M13.5 10.5V6.75a4.5 4.5 0 119 0v3.75m-.75 11.25h10.5a2.25 2.25 0 002.25-2.25v-6.75a2.25 2.25 0 00-2.25-2.25H6.75a2.25 2.25 0 00-2.25 2.25v6.75a2.25 2.25 0 002.25 2.25z" />
  </svg>
)

const FlameIcon = ({ className = 'icon-md' }: IconProps) => (
  <svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" strokeWidth={1.5} stroke="currentColor" className={`icon-svg ${className}`}>
    <path strokeLinecap="round" strokeLinejoin="round" d="M15.362 5.214A8.252 8.252 0 0112 21 8.25 8.25 0 016.038 7.048 8.287 8.287 0 009 9.6a8.983 8.983 0 013.361-6.867 8.21 8.21 0 003 2.48z" />
    <path strokeLinecap="round" strokeLinejoin="round" d="M12 18a3.75 3.75 0 00.495-7.467 5.99 5.99 0 00-1.925 3.546 5.974 5.974 0 01-2.133-1A3.75 3.75 0 0012 18z" />
  </svg>
)

// HOLD COUNTDOWN TIMER COMPONENT
type HoldTimerProps = {
  expiresAt?: string
  onExpire?: () => void
}

function HoldTimer({ expiresAt, onExpire }: HoldTimerProps) {
  const [secondsLeft, setSecondsLeft] = useState<number>(0)
  const totalDuration = 300 // assume 5 mins hold limit

  useEffect(() => {
    if (!expiresAt) return

    const calculateTime = () => {
      const diff = new Date(expiresAt).getTime() - Date.now()
      const secs = Math.max(0, Math.floor(diff / 1000))
      setSecondsLeft(secs)
      
      if (secs === 0 && onExpire) {
        onExpire()
      }
    }

    calculateTime()
    const timer = setInterval(calculateTime, 1000)

    return () => clearInterval(timer)
  }, [expiresAt, onExpire])

  if (!expiresAt) {
    return (
      <div className="countdown-wrapper">
        <div className="countdown-text-row">
          <span className="countdown-label">Expires</span>
          <span className="countdown-timer">Indefinite</span>
        </div>
      </div>
    )
  }

  const ratio = Math.min(1, Math.max(0, secondsLeft / totalDuration))
  let progressClass = 'success'
  let dotClass = ''

  if (secondsLeft === 0) {
    progressClass = 'expired'
    dotClass = 'expired'
  } else if (ratio < 0.15) {
    progressClass = 'danger'
    dotClass = 'danger'
  } else if (ratio < 0.5) {
    progressClass = 'warning'
    dotClass = 'warning'
  }

  const formatTime = (totalSecs: number) => {
    if (totalSecs <= 0) return 'Expired'
    const mins = Math.floor(totalSecs / 60)
    const secs = totalSecs % 60
    return `${mins}m ${secs}s`
  }

  return (
    <div className="countdown-wrapper">
      <div className="countdown-text-row">
        <span className="countdown-label">Time Remaining</span>
        <span className="countdown-timer">
          <span className={`timer-dot ${dotClass}`} />
          {formatTime(secondsLeft)}
        </span>
      </div>
      <div className="progress-bar-container">
        <div 
          className={`progress-bar-fill ${progressClass}`} 
          style={{ width: `${ratio * 100}%` }}
        />
      </div>
    </div>
  )
}

function App() {
  const [inventory, setInventory] = useState<InventoryItem[]>([])
  const [holds, setHolds] = useState<Hold[]>([])
  const [sku, setSku] = useState('')
  const [qty, setQty] = useState(1)
  const [searchQuery, setSearchQuery] = useState('')
  const [loading, setLoading] = useState(false)
  const [toasts, setToasts] = useState<Toast[]>([])

  useEffect(() => {
    loadData()
  }, [])

  // Toast triggers
  function addToast(title: string, desc: string, type: Toast['type'] = 'success') {
    const id = Math.random().toString(36).substring(2, 9)
    setToasts((current) => [...current, { id, title, desc, type }])
    setTimeout(() => {
      removeToast(id)
    }, 4000)
  }

  function removeToast(id: string) {
    setToasts((current) => current.filter((t) => t.id !== id))
  }

  async function loadData() {
    setLoading(true)
    try {
      await Promise.all([loadInventory(), loadActiveHolds()])
    } catch (e) {
      console.error(e)
    } finally {
      setLoading(false)
    }
  }

  async function loadInventory() {
    try {
      const data = await fetchInventory()
      setInventory(data)
      // If the selected sku isn't set yet, or is not in inventory anymore, select the first valid sku
      if (data.length > 0 && !sku) {
        setSku(data[0].sku)
      }
    } catch (e) {
      addToast('Inventory Error', 'Failed to load current inventory products.', 'error')
    }
  }

  async function loadActiveHolds() {
    try {
      const data = await fetchActiveHolds()
      setHolds(data)
    } catch (e) {
      addToast('Reservations Error', 'Failed to load active hold reservations.', 'error')
    }
  }

  async function handleCreateHold(e: React.FormEvent) {
    e.preventDefault()
    if (!sku) {
      addToast('Selection Required', 'Please select a product SKU first.', 'warning')
      return
    }

    const selectedItem = inventory.find((it) => it.sku === sku)
    if (!selectedItem) {
      addToast('Invalid SKU', 'The selected product does not exist in inventory.', 'error')
      return
    }

    if (qty > selectedItem.availableQuantity) {
      addToast('Insufficient Stock', `Only ${selectedItem.availableQuantity} units available for ${sku}.`, 'warning')
      return
    }

    try {
      const newHold = await createHold({ sku, quantity: Number(qty), expiresInSeconds: 300 })
      setHolds((h) => [newHold, ...h])
      addToast('Hold Secured', `Reserved ${qty} units of ${sku} for 5 minutes.`, 'success')
      loadInventory() // update stock
    } catch (e) {
      const errMsg = (e as any)?.message || 'Failed to request reserve hold'
      addToast('Hold Request Failed', errMsg, 'error')
    }
  }

  async function handleRelease(id: string, holdSku: string) {
    try {
      await releaseHold(id)
      setHolds((h) => h.filter((x) => x.id !== id))
      addToast('Release Complete', `Released reservation for ${holdSku}.`, 'success')
      loadInventory() // update stock
    } catch (e) {
      const errMsg = (e as any)?.message || 'Failed to release reservation'
      addToast('Release Failed', errMsg, 'error')
    }
  }

  // Quick select SKU from table row click
  function selectSku(item: InventoryItem) {
    if (item.availableQuantity <= 0) {
      addToast('Out of Stock', `${item.name} is currently out of stock. Cannot hold.`, 'warning')
      return
    }
    setSku(item.sku)
    setQty(Math.min(1, item.availableQuantity))
    addToast('Product Selected', `Set active form target to SKU: ${item.sku}.`, 'success')
  }

  // Handle selected SKU change from form dropdown
  function handleSkuChange(newSku: string) {
    setSku(newSku)
    const item = inventory.find((it) => it.sku === newSku)
    if (item) {
      setQty(Math.min(qty === 0 ? 1 : qty, item.availableQuantity))
    }
  }

  // Filter inventory
  const filteredInventory = inventory.filter(
    (it) =>
      it.sku.toLowerCase().includes(searchQuery.toLowerCase()) ||
      it.name.toLowerCase().includes(searchQuery.toLowerCase())
  )

  const selectedProduct = inventory.find((it) => it.sku === sku)
  const maxAvailable = selectedProduct ? selectedProduct.availableQuantity : 0

  // Calc Summary Statistics
  const totalSkuCount = inventory.length
  const totalStockCount = inventory.reduce((acc, curr) => acc + curr.availableQuantity, 0)
  const activeHoldsCount = holds.length
  const totalHeldQty = holds.reduce((acc, curr) => acc + curr.quantity, 0)

  return (
    <div className="app-container">
      {/* HEADER SECTION */}
      <header className="dashboard-header">
        <div className="brand">
          <div className="brand-icon">
            <PackageIcon className="icon-xl" />
          </div>
          <div className="brand-text">
            <h1>InventoryHold</h1>
            <p>Real-Time Stock Allocation & Reservation Engine</p>
          </div>
        </div>
        <div className="header-actions">
          <button 
            className="btn-refresh" 
            onClick={loadData} 
            disabled={loading}
            title="Refresh Data"
            aria-label="Refresh Data"
          >
            <RefreshCwIcon className="icon-sm" />
          </button>
        </div>
      </header>

      {/* STATS OVERVIEW ROW */}
      <section className="stats-grid">
        <div className="stat-card">
          <div className="stat-icon">
            <LayersIcon className="icon-lg" />
          </div>
          <div className="stat-info">
            <span className="stat-value">{totalSkuCount}</span>
            <span className="stat-label">Unique Products</span>
          </div>
        </div>
        <div className="stat-card">
          <div className="stat-icon success">
            <ShieldCheckIcon className="icon-lg" />
          </div>
          <div className="stat-info">
            <span className="stat-value">{totalStockCount}</span>
            <span className="stat-label">Available Stock</span>
          </div>
        </div>
        <div className="stat-card">
          <div className="stat-icon warning">
            <LockIcon className="icon-lg" />
          </div>
          <div className="stat-info">
            <span className="stat-value">{activeHoldsCount}</span>
            <span className="stat-label">Active Holds</span>
          </div>
        </div>
        <div className="stat-card">
          <div className="stat-icon warning">
            <FlameIcon className="icon-lg" />
          </div>
          <div className="stat-info">
            <span className="stat-value">{totalHeldQty}</span>
            <span className="stat-label">Total Held Units</span>
          </div>
        </div>
      </section>

      {/* MAIN CONTENT GRID */}
      <main className="dashboard-content">
        
        {/* LEFT COLUMN: Inventory Grid & Create Hold Form */}
        <div style={{ display: 'flex', flexDirection: 'column', gap: '24px' }}>
          
          {/* Inventory Table Card */}
          <div className="dashboard-card">
            <div className="card-header">
              <h2 className="card-title">
                <PackageIcon className="icon-md" /> Current Inventory
              </h2>
              <div className="search-wrapper">
                <SearchIcon className="icon-sm" />
                <input 
                  type="text" 
                  className="search-input" 
                  placeholder="Search SKU or Name..."
                  value={searchQuery}
                  onChange={(e) => setSearchQuery(e.target.value)}
                />
              </div>
            </div>

            <div className="table-container">
              <table className="modern-table">
                <thead>
                  <tr>
                    <th>SKU Code</th>
                    <th>Product Name</th>
                    <th>Available Quantity</th>
                    <th>Status</th>
                    <th style={{ textAlign: 'right' }}>Actions</th>
                  </tr>
                </thead>
                <tbody>
                  {filteredInventory.length === 0 ? (
                    <tr>
                      <td colSpan={5}>
                        <div className="empty-state">
                          <PackageIcon className="icon-xl" />
                          <p>No inventory items match your search.</p>
                        </div>
                      </td>
                    </tr>
                  ) : (
                    filteredInventory.map((it) => {
                      const qty = it.availableQuantity
                      let badgeClass = 'in-stock'
                      let statusText = 'In Stock'
                      if (qty === 0) {
                        badgeClass = 'out-of-stock'
                        statusText = 'Out of Stock'
                      } else if (qty <= 5) {
                        badgeClass = 'low-stock'
                        statusText = 'Low Stock'
                      }

                      return (
                        <tr 
                          key={it.sku} 
                          onClick={() => selectSku(it)}
                          style={{
                            backgroundColor: sku === it.sku ? 'rgba(99, 102, 241, 0.08)' : undefined,
                            borderLeft: sku === it.sku ? '3px solid var(--accent)' : undefined
                          }}
                        >
                          <td>
                            <span className="sku-cell">{it.sku}</span>
                          </td>
                          <td style={{ fontWeight: 600 }}>{it.name}</td>
                          <td className="quantity-cell">{qty}</td>
                          <td>
                            <span className={`stock-badge ${badgeClass}`}>
                              <span className="badge-dot" />
                              {statusText}
                            </span>
                          </td>
                          <td style={{ textAlign: 'right' }} onClick={(e) => e.stopPropagation()}>
                            <button 
                              className="btn-table-action" 
                              onClick={() => selectSku(it)}
                              disabled={qty <= 0}
                            >
                              Select
                            </button>
                          </td>
                        </tr>
                      )
                    })
                  )}
                </tbody>
              </table>
            </div>
          </div>

          {/* Create Hold Form Card */}
          <div className="dashboard-card">
            <div className="card-header">
              <h2 className="card-title">
                <LockIcon className="icon-md" /> Create Inventory Reservation
              </h2>
            </div>
            
            <form onSubmit={handleCreateHold} className="form-grid">
              
              {/* Product SKU Selector */}
              <div className="form-group">
                <label className="form-label" htmlFor="sku-select">Target Product SKU</label>
                <div className="form-input-wrapper">
                  <PackageIcon className="icon-sm" />
                  <select 
                    id="sku-select"
                    className="form-select"
                    value={sku}
                    onChange={(e) => handleSkuChange(e.target.value)}
                  >
                    <option value="" disabled>Select a SKU...</option>
                    {inventory.map((item) => (
                      <option key={item.sku} value={item.sku} disabled={item.availableQuantity === 0}>
                        {item.sku} - {item.name} ({item.availableQuantity} available)
                      </option>
                    ))}
                  </select>
                  <ChevronDownIcon className="icon-xs" />
                </div>
              </div>

              {/* Reservation quantity slider / counter */}
              <div className="form-group">
                <label className="form-label">Reservation Quantity</label>
                <div className="slider-container">
                  <div className="slider-header">
                    <span>Units to Reserve</span>
                    <span className="slider-value">{qty}</span>
                  </div>
                  <input 
                    type="range"
                    className="quantity-slider"
                    min={maxAvailable > 0 ? 1 : 0}
                    max={maxAvailable}
                    value={qty}
                    disabled={maxAvailable === 0}
                    onChange={(e) => setQty(Number(e.target.value))}
                  />
                  <div className="slider-labels">
                    <span>{maxAvailable > 0 ? 1 : 0}</span>
                    <span>Max: {maxAvailable}</span>
                  </div>
                </div>
              </div>

              <div className="form-group full-width">
                <button 
                  type="submit" 
                  className="btn-submit"
                  disabled={maxAvailable === 0 || qty === 0 || !sku}
                >
                  <LockIcon className="icon-sm" /> Secure {qty} Unit{qty !== 1 ? 's' : ''} Reservation
                </button>
              </div>

            </form>
          </div>

        </div>

        {/* RIGHT COLUMN: Active Holds */}
        <div className="dashboard-card">
          <div className="card-header">
            <h2 className="card-title">
              <ClockIcon className="icon-md" /> Active Reservations
            </h2>
            <span 
              className="stock-badge in-stock" 
              style={{ padding: '2px 8px', fontSize: '0.7rem' }}
            >
              5 Min TTL
            </span>
          </div>

          <div className="holds-list">
            {holds.length === 0 ? (
              <div className="empty-state">
                <ClockIcon className="icon-xl" />
                <p>No active reservations found.</p>
                <span style={{ fontSize: '0.8rem', color: 'var(--color-muted)' }}>
                  Use the reservation panel to hold inventory.
                </span>
              </div>
            ) : (
              holds.map((h) => (
                <div className="hold-card" key={h.id}>
                  <div className="hold-card-top">
                    <div className="hold-meta">
                      <span className="hold-sku">{h.sku}</span>
                      <span className="hold-qty">{h.quantity} Unit{h.quantity !== 1 ? 's' : ''} Locked</span>
                    </div>
                    <button 
                      className="btn-release"
                      onClick={() => handleRelease(h.id, h.sku)}
                    >
                      <UnlockIcon className="icon-xs" /> Release
                    </button>
                  </div>
                  
                  <HoldTimer 
                    expiresAt={h.expiresAt} 
                    onExpire={() => {
                      // refresh when an item expires to sync the state
                      loadInventory()
                    }} 
                  />
                </div>
              ))
            )}
          </div>
        </div>

      </main>

      {/* CUSTOM TOAST CONTAINER */}
      <div className="toast-container">
        {toasts.map((toast) => (
          <div className={`toast ${toast.type}`} key={toast.id}>
            <div className="toast-content">
              <div className="toast-title">{toast.title}</div>
              <div className="toast-desc">{toast.desc}</div>
            </div>
            <button className="toast-close" onClick={() => removeToast(toast.id)}>
              <XIcon className="icon-xs" />
            </button>
          </div>
        ))}
      </div>
    </div>
  )
}

export default App
