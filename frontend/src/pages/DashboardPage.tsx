import { useCallback, useEffect, useMemo, useState } from 'react'
import {
  Alert,
  Button,
  Card,
  Col,
  Row,
  Statistic,
  Table,
  Tag,
  Typography,
  message,
  type TableColumnsType,
} from 'antd'
import {
  AppstoreOutlined,
  CarOutlined,
  DollarOutlined,
  ReloadOutlined,
  ShoppingCartOutlined,
  TeamOutlined,
  WarningOutlined,
} from '@ant-design/icons'
import { Bar, Column, Pie } from '@ant-design/charts'
import PageHeader from '../components/PageHeader'
import { dashboardApi, getApiErrorMessage } from '../services/api'
import type { DashboardData, RestockSuggestion } from '../types'
import { ORDER_STATUS_LABELS } from '../utils/constants'
import { formatCurrency, formatNumber } from '../utils/format'

export default function DashboardPage() {
  const [data, setData] = useState<DashboardData | null>(null)
  const [loading, setLoading] = useState(true)

  const loadData = useCallback(async () => {
    setLoading(true)
    try {
      setData(await dashboardApi.getAll())
    } catch (error) {
      message.error(getApiErrorMessage(error, 'Không thể tải dữ liệu tổng quan.'))
    } finally {
      setLoading(false)
    }
  }, [])

  useEffect(() => {
    loadData()
  }, [loadData])

  const statusChartData = useMemo(
    () =>
      (data?.ordersByStatus ?? []).map((item) => ({
        ...item,
        label: ORDER_STATUS_LABELS[item.status] ?? item.status,
      })),
    [data],
  )

  const topProductData = useMemo(
    () =>
      (data?.topProducts ?? []).map((item) => ({
        ...item,
        label:
          item.productName.length > 24
            ? `${item.productName.slice(0, 24)}…`
            : item.productName,
      })),
    [data],
  )

  const restockColumns: TableColumnsType<RestockSuggestion> = [
    {
      title: 'Sản phẩm',
      dataIndex: 'productName',
      render: (value, record) => (
        <div>
          <Typography.Text strong>{value}</Typography.Text>
          <div className="table-secondary">{record.productCode}</div>
        </div>
      ),
    },
    {
      title: 'Tồn hiện tại',
      dataIndex: 'stockQuantity',
      align: 'right',
      width: 130,
      render: (value) => formatNumber(value),
    },
    {
      title: 'Tồn tối thiểu',
      dataIndex: 'minimumStock',
      align: 'right',
      width: 130,
      render: (value) => formatNumber(value),
    },
    {
      title: 'Bán TB/ngày',
      dataIndex: 'averageDailySales',
      align: 'right',
      width: 140,
      render: (value) => formatNumber(value, 2),
    },
    {
      title: 'Ngày còn hàng',
      dataIndex: 'daysRemaining',
      align: 'right',
      width: 140,
      render: (value: number | null) =>
        value == null ? 'Chưa có doanh số' : `${formatNumber(value, 1)} ngày`,
    },
    {
      title: 'Khuyến nghị',
      key: 'warning',
      width: 160,
      render: (_, record) =>
        record.needsRestock ? (
          <Tag color="error" icon={<WarningOutlined />}>
            Cần nhập thêm
          </Tag>
        ) : record.isBelowMinimum ? (
          <Tag color="warning">Dưới mức tối thiểu</Tag>
        ) : (
          <Tag color="success">Đủ hàng</Tag>
        ),
    },
  ]

  const summary = data?.summary
  const cards = [
    {
      title: 'Tổng sản phẩm',
      value: summary?.totalProducts ?? 0,
      icon: <AppstoreOutlined />,
      tone: 'blue',
    },
    {
      title: 'Tổng khách hàng',
      value: summary?.totalCustomers ?? 0,
      icon: <TeamOutlined />,
      tone: 'cyan',
    },
    {
      title: 'Tổng đơn hàng',
      value: summary?.totalOrders ?? 0,
      icon: <ShoppingCartOutlined />,
      tone: 'indigo',
    },
    {
      title: 'Đơn mới',
      value: summary?.newOrders ?? 0,
      icon: <ShoppingCartOutlined />,
      tone: 'orange',
    },
    {
      title: 'Đang giao',
      value: summary?.deliveringOrders ?? 0,
      icon: <CarOutlined />,
      tone: 'purple',
    },
    {
      title: 'Đã giao thành công',
      value: summary?.deliveredOrders ?? 0,
      icon: <CarOutlined />,
      tone: 'green',
    },
    {
      title: 'Doanh thu tháng',
      value: summary?.monthlyRevenue ?? 0,
      icon: <DollarOutlined />,
      tone: 'emerald',
      formatter: formatCurrency,
    },
    {
      title: 'Sản phẩm sắp hết',
      value: summary?.lowStockProducts ?? 0,
      icon: <WarningOutlined />,
      tone: 'red',
    },
  ]

  return (
    <>
      <PageHeader
        title="Tổng quan"
        subtitle="Tình hình kinh doanh và vận hành giao nước Gia Phú."
        actions={
          <Button icon={<ReloadOutlined />} loading={loading} onClick={loadData}>
            Làm mới
          </Button>
        }
      />
      <Row gutter={[16, 16]} className="stats-grid">
        {cards.map((item) => (
          <Col xs={24} sm={12} lg={6} key={item.title}>
            <Card loading={loading} className={`stat-card stat-${item.tone}`}>
              <div className="stat-card-icon">{item.icon}</div>
              <Statistic
                title={item.title}
                value={item.value}
                formatter={
                  item.formatter
                    ? (value) => item.formatter?.(Number(value))
                    : undefined
                }
              />
            </Card>
          </Col>
        ))}
      </Row>

      <Row gutter={[16, 16]} className="dashboard-charts">
        <Col xs={24} xl={14}>
          <Card title="Doanh thu 12 tháng gần nhất" loading={loading}>
            {data?.revenueByMonth?.length ? (
              <Column
                height={320}
                data={data.revenueByMonth}
                xField="label"
                yField="revenue"
                colorField="#087ea4"
                axis={{
                  y: {
                    labelFormatter: (value: number) =>
                      `${formatNumber(Number(value) / 1_000_000, 1)} tr`,
                  },
                }}
                tooltip={{
                  title: (datum: { label: string }) => datum.label,
                  items: [
                    {
                      channel: 'y',
                      name: 'Doanh thu',
                      valueFormatter: (value: number) =>
                        formatCurrency(Number(value)),
                    },
                  ],
                }}
              />
            ) : (
              <Alert
                type="info"
                showIcon
                message="Chưa có dữ liệu doanh thu."
              />
            )}
          </Card>
        </Col>
        <Col xs={24} xl={10}>
          <Card title="Đơn hàng theo trạng thái" loading={loading}>
            {statusChartData.length ? (
              <Pie
                height={320}
                data={statusChartData}
                angleField="count"
                colorField="label"
                innerRadius={0.58}
                label={{
                  text: (datum: { label: string; count: number }) =>
                    `${datum.label}: ${datum.count}`,
                  position: 'outside',
                }}
                legend={{ color: { position: 'bottom', layout: { justifyContent: 'center' } } }}
              />
            ) : (
              <Alert type="info" showIcon message="Chưa có dữ liệu đơn hàng." />
            )}
          </Card>
        </Col>
        <Col xs={24}>
          <Card title="5 sản phẩm bán chạy nhất" loading={loading}>
            {topProductData.length ? (
              <Bar
                height={300}
                data={topProductData}
                xField="quantitySold"
                yField="label"
                colorField="#06a77d"
                label={{ text: 'quantitySold', position: 'right' }}
                axis={{ x: { title: 'Số lượng đã bán' } }}
                tooltip={{
                  items: [
                    {
                      channel: 'x',
                      name: 'Số lượng',
                    },
                  ],
                }}
              />
            ) : (
              <Alert type="info" showIcon message="Chưa có dữ liệu bán hàng." />
            )}
          </Card>
        </Col>
      </Row>

      <Card
        title="Gợi ý sản phẩm cần nhập thêm"
        className="content-card restock-card"
        loading={loading}
        extra={
          <Typography.Text type="secondary">
            Dựa trên doanh số 30 ngày gần nhất
          </Typography.Text>
        }
      >
        <Table
          rowKey="productId"
          columns={restockColumns}
          dataSource={data?.restockSuggestions ?? []}
          pagination={false}
          scroll={{ x: 900 }}
          rowClassName={(record) =>
            record.needsRestock ? 'low-stock-row' : ''
          }
        />
      </Card>
    </>
  )
}
