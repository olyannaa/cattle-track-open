import { Col, Flex, Row } from 'antd';
import { CalvingChart } from './components/calving-chart/CalvingChart';
import styles from './RecordsPage.module.css';
import { DailyWeightGainChart } from './components/daily-weight-gain-chart/DailyWeightGainChart';
import { Birth12WeightChart } from './components/birth-12-weight-chart/Birth12WeightChart';
import { PregnancyChart } from './components/pregnancy-chart/PregnancyChart';
import { VaccinationsChart } from './components/vaccinations-chart/VaccinationsChart';
import { BloodTestsChart } from './components/blood-tests-chart/BloodTestsChart';
import { BirthWeightChart } from './components/birth-weight-chart/BirthWeightChart';

export const RecordsPage = () => {
    return (
        <Flex className={styles.page} vertical gap={24}>
            <Row gutter={[24, 24]}>
                <Col lg={12} xs={24}>
                    <CalvingChart />
                </Col>
                <Col lg={12} xs={24}>
                    <BirthWeightChart />
                </Col>
            </Row>
            <Row gutter={[24, 24]}>
                <Col lg={12} xs={24}>
                    <DailyWeightGainChart />
                </Col>
                <Col lg={12} xs={24}>
                    <Birth12WeightChart />
                </Col>
            </Row>
            <Row>
                <Col span={24}>
                    <PregnancyChart />
                </Col>
            </Row>
            <Row gutter={[24, 24]}>
                <Col lg={12} xs={24}>
                    <VaccinationsChart />
                </Col>
                <Col lg={12} xs={24}>
                    <BloodTestsChart />
                </Col>
            </Row>
        </Flex>
    );
};
