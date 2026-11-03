import { useCallback, useEffect, useState } from 'react'
import { Button, Card, Descriptions, Drawer, Input, Select, Space, Table, Tag, Typography, message, type TableColumnsType } from 'antd'
import { EyeOutlined, ReloadOutlined, SearchOutlined } from '@ant-design/icons'
import PageHeader from '../components/PageHeader'
import { auditApi, getApiErrorMessage } from '../services/api'
import type { AuditLog } from '../types'
import { PAGE_SIZE_OPTIONS } from '../utils/constants'
import { formatDateTime } from '../utils/format'

const prettyJson = (value?: string | null) => {
  if (!value) return '—'
  try { return JSON.stringify(JSON.parse(value), null, 2) } catch { return value }
}

export default function AuditPage() {
  const [items, setItems] = useState<AuditLog[]>([])
  const [selected, setSelected] = useState<AuditLog>()
  const [loading, setLoading] = useState(false)
  const [searchDraft, setSearchDraft] = useState('')
  const [search, setSearch] = useState('')
  const [entityType, setEntityType] = useState<string>()
  const [page, setPage] = useState(1)
  const [pageSize, setPageSize] = useState(10)
  const [total, setTotal] = useState(0)

  const load = useCallback(async () => {
    try {
      setLoading(true)
      const result = await auditApi.list({ page, pageSize, search: search || undefined, entityType })
      setItems(result.items)
      setTotal(result.totalCount)
    } catch (error) {
      message.error(getApiErrorMessage(error, 'Không thể tải nhật ký hệ thống.'))
    } finally { setLoading(false) }
  }, [entityType, page, pageSize, search])

  useEffect(() => { load() }, [load])

  const columns: TableColumnsType<AuditLog> = [
    { title: 'Thời gian', dataIndex: 'createdAt', width: 170, render: formatDateTime },
    { title: 'Người thao tác', dataIndex: 'actorName', width: 170 },
    { title: 'Hành động', dataIndex: 'action', width: 190, render: (value) => <Tag color="blue">{value}</Tag> },
    { title: 'Đối tượng', width: 180, render: (_, row) => `${row.entityType}${row.entityId ? ` #${row.entityId}` : ''}` },
    { title: 'IP', dataIndex: 'ipAddress', width: 140, render: (value) => value || '—' },
    { title: '', fixed: 'right', width: 70, render: (_, row) => <Button type="text" icon={<EyeOutlined />} onClick={() => setSelected(row)} /> },
  ]

  return (
    <>
      <PageHeader title="Nhật ký hệ thống" subtitle="Tra cứu các thay đổi quan trọng để kiểm soát và đối soát vận hành." />
      <Card className="content-card">
        <div className="table-toolbar">
          <Space wrap>
            <Input value={searchDraft} prefix={<SearchOutlined />} allowClear placeholder="Người dùng, hành động, mã..." onChange={(event) => setSearchDraft(event.target.value)} onPressEnter={() => { setPage(1); setSearch(searchDraft.trim()) }} style={{ width: 280 }} />
            <Select allowClear placeholder="Loại đối tượng" value={entityType} onChange={(value) => { setPage(1); setEntityType(value) }} style={{ width: 170 }} options={['Order', 'Delivery', 'Product', 'Customer'].map((value) => ({ value, label: value }))} />
            <Button icon={<SearchOutlined />} onClick={() => { setPage(1); setSearch(searchDraft.trim()) }}>Tìm kiếm</Button>
          </Space>
          <Button icon={<ReloadOutlined />} onClick={load}>Làm mới</Button>
        </div>
        <Table rowKey="id" loading={loading} columns={columns} dataSource={items} scroll={{ x: 950 }} pagination={{ current: page, pageSize, total, showSizeChanger: true, pageSizeOptions: PAGE_SIZE_OPTIONS, onChange: (next, size) => { setPage(size !== pageSize ? 1 : next); setPageSize(size) } }} />
      </Card>
      <Drawer title="Chi tiết nhật ký" open={Boolean(selected)} onClose={() => setSelected(undefined)} width={680}>
        {selected && <>
          <Descriptions bordered column={1} size="small">
            <Descriptions.Item label="Thời gian">{formatDateTime(selected.createdAt)}</Descriptions.Item>
            <Descriptions.Item label="Người thao tác">{selected.actorName}</Descriptions.Item>
            <Descriptions.Item label="Hành động">{selected.action}</Descriptions.Item>
            <Descriptions.Item label="Đối tượng">{selected.entityType} #{selected.entityId || '—'}</Descriptions.Item>
            <Descriptions.Item label="Địa chỉ IP">{selected.ipAddress || '—'}</Descriptions.Item>
          </Descriptions>
          <Typography.Title level={5} className="drawer-section-title">Dữ liệu trước</Typography.Title>
          <pre className="audit-json">{prettyJson(selected.oldValues)}</pre>
          <Typography.Title level={5} className="drawer-section-title">Dữ liệu sau</Typography.Title>
          <pre className="audit-json">{prettyJson(selected.newValues)}</pre>
        </>}
      </Drawer>
    </>
  )
}
