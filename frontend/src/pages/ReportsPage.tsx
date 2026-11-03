import { useState } from 'react'
import { Button, Card, Col, DatePicker, Row, Space, Typography, message } from 'antd'
import { BarChartOutlined, CarOutlined, DollarOutlined, DownloadOutlined, InboxOutlined } from '@ant-design/icons'
import dayjs, { type Dayjs } from 'dayjs'
import PageHeader from '../components/PageHeader'
import { getApiErrorMessage, reportApi } from '../services/api'

type ReportKey = 'revenue' | 'debts' | 'inventory' | 'delivery'

export default function ReportsPage() {
  const [range, setRange] = useState<[Dayjs, Dayjs]>([dayjs().startOf('month'), dayjs()])
  const [downloading, setDownloading] = useState<ReportKey>()

  const download = async (key: ReportKey) => {
    try {
      setDownloading(key)
      const from = range[0].format('YYYY-MM-DD')
      const to = range[1].format('YYYY-MM-DD')
      const result = key === 'revenue' ? await reportApi.revenue(from, to)
        : key === 'debts' ? await reportApi.debts()
          : key === 'inventory' ? await reportApi.inventory()
            : await reportApi.deliveryPerformance(from, to)
      const url = URL.createObjectURL(result.blob)
      const anchor = document.createElement('a')
      anchor.href = url
      anchor.download = result.fileName || `bao-cao-${key}.xlsx`
      anchor.click()
      URL.revokeObjectURL(url)
      message.success('Đã tạo báo cáo Excel.')
    } catch (error) {
      message.error(getApiErrorMessage(error, 'Không thể xuất báo cáo.'))
    } finally {
      setDownloading(undefined)
    }
  }

  const reports = [
    { key: 'revenue' as const, title: 'Doanh thu', text: 'Đơn giao thành công, tiền hàng, tiền cọc và số đã thu.', icon: <BarChartOutlined /> },
    { key: 'debts' as const, title: 'Công nợ', text: 'Danh sách đơn đã giao còn số tiền phải thu.', icon: <DollarOutlined /> },
    { key: 'inventory' as const, title: 'Tồn kho', text: 'Tồn hiện tại, định mức và sản phẩm cần cảnh báo.', icon: <InboxOutlined /> },
    { key: 'delivery' as const, title: 'Hiệu suất giao hàng', text: 'Tỷ lệ thành công và thời gian giao trung bình theo nhân viên.', icon: <CarOutlined /> },
  ]

  return (
    <>
      <PageHeader title="Báo cáo" subtitle="Xuất dữ liệu vận hành ra tệp Excel để đối soát và lưu trữ." />
      <Card className="content-card report-filter-card">
        <Space wrap>
          <Typography.Text strong>Khoảng thời gian:</Typography.Text>
          <DatePicker.RangePicker
            value={range}
            format="DD/MM/YYYY"
            allowClear={false}
            onChange={(value) => value?.[0] && value[1] && setRange([value[0], value[1]])}
          />
          <Typography.Text type="secondary">Áp dụng cho doanh thu và hiệu suất giao hàng.</Typography.Text>
        </Space>
      </Card>
      <Row gutter={[16, 16]}>
        {reports.map((report) => (
          <Col xs={24} md={12} key={report.key}>
            <Card className="content-card report-card">
              <Space align="start" size={16}>
                <div className="report-card-icon">{report.icon}</div>
                <div>
                  <Typography.Title level={4}>{report.title}</Typography.Title>
                  <Typography.Paragraph type="secondary">{report.text}</Typography.Paragraph>
                  <Button
                    type="primary"
                    icon={<DownloadOutlined />}
                    loading={downloading === report.key}
                    onClick={() => download(report.key)}
                  >
                    Tải Excel
                  </Button>
                </div>
              </Space>
            </Card>
          </Col>
        ))}
      </Row>
    </>
  )
}
