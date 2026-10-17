import 'package:flutter/material.dart';
import '../models/reading_models.dart';
import '../widgets/mochi_theme.dart';
import '../services/reading_api.dart';
import 'review_screen.dart';

class ResultScreen extends StatelessWidget {
  const ResultScreen({super.key, required this.result, required this.api});
  final AttemptInfo result;
  final ReadingApi api;
  @override
  Widget build(BuildContext context) => Scaffold(
    appBar: AppBar(title: const Text('Test Results Summary')),
    body: MobilePage(
      child: ListView(
        padding: const EdgeInsets.all(16),
        children: [
          MochiCard(
            color: const Color(0xffe8f5f2),
            child: Row(
              children: [
                const Expanded(
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Pill(
                        'TUYỆT VỜI ✧',
                        color: Colors.white,
                        foreground: Mochi.correct,
                      ),
                      SizedBox(height: 12),
                      Text(
                        'Hoàn thành rồi!',
                        style: TextStyle(
                          fontSize: 24,
                          fontWeight: FontWeight.w600,
                        ),
                      ),
                      SizedBox(height: 8),
                      Text(
                        'Bạn đã cán đích một hành trình Reading. Cùng nhìn lại kết quả nhé!',
                        style: TextStyle(fontSize: 12),
                      ),
                    ],
                  ),
                ),
                Image.asset('assets/mochi.png', width: 92, height: 92),
              ],
            ),
          ),
          const SizedBox(height: 18),
          Wrap(
            spacing: 8,
            runSpacing: 8,
            children: [
              Pill('Lượt thi #${result.id}'),
              Pill(
                result.status == 'EXPIRED'
                    ? 'Hệ thống tự thu bài'
                    : 'Đã nộp bài',
                icon: Icons.verified_outlined,
              ),
            ],
          ),
          const SizedBox(height: 18),
          MochiCard(
            child: Column(
              children: [
                Text(
                  result.title,
                  textAlign: TextAlign.center,
                  style: const TextStyle(fontWeight: FontWeight.w600),
                ),
                const SizedBox(height: 28),
                SizedBox(
                  width: 155,
                  height: 155,
                  child: Stack(
                    alignment: Alignment.center,
                    children: [
                      SizedBox.expand(
                        child: CircularProgressIndicator(
                          value: (result.accuracy ?? 0) / 100,
                          strokeWidth: 12,
                          backgroundColor: const Color(0xffffe8f0),
                          color: Mochi.correct,
                        ),
                      ),
                      Column(
                        mainAxisSize: MainAxisSize.min,
                        children: [
                          Text(
                            '${(result.accuracy ?? 0).toStringAsFixed(0)}%',
                            style: const TextStyle(
                              fontSize: 38,
                              fontWeight: FontWeight.w600,
                            ),
                          ),
                          const Text(
                            'TỶ LỆ CHÍNH XÁC',
                            style: TextStyle(fontSize: 9, color: Mochi.muted),
                          ),
                        ],
                      ),
                    ],
                  ),
                ),
                const SizedBox(height: 28),
                Row(
                  children: [
                    _Metric(
                      '${result.correct}',
                      'Câu đúng',
                      Mochi.correct,
                      const Color(0xffecfaf5),
                    ),
                    const SizedBox(width: 8),
                    _Metric(
                      '${result.wrong}',
                      'Câu sai',
                      Mochi.wrong,
                      const Color(0xffffe8f0),
                    ),
                    const SizedBox(width: 8),
                    _Metric(
                      '${result.skipped}',
                      'Bỏ qua',
                      Mochi.muted,
                      Mochi.lavender,
                    ),
                  ],
                ),
                const SizedBox(height: 18),
                Text(
                  'Tổng cộng ${result.total} câu · Kết quả đã lưu',
                  style: const TextStyle(fontSize: 12, color: Mochi.muted),
                ),
              ],
            ),
          ),
          const SizedBox(height: 18),
          if (result.partResults.isNotEmpty) ...[
            const MochiCard(
              child: Text(
                'Phân tích theo phần thi',
                style: TextStyle(fontSize: 18, fontWeight: FontWeight.w600),
              ),
            ),
            const SizedBox(height: 12),
            for (final part in result.partResults)
              Padding(
                padding: const EdgeInsets.only(bottom: 12),
                child: MochiCard(
                  color: part.part == 5
                      ? Mochi.lavender
                      : part.part == 6
                      ? const Color(0xffecfaf5)
                      : const Color(0xfffff1e9),
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Text(
                        'Part ${part.part} · ${part.part == 5
                            ? 'Incomplete Sentences'
                            : part.part == 6
                            ? 'Text Completion'
                            : 'Reading Comprehension'}',
                        style: const TextStyle(
                          fontSize: 13,
                          fontWeight: FontWeight.w600,
                        ),
                      ),
                      const SizedBox(height: 8),
                      Text(
                        '${part.correct}/${part.total} đúng · ${part.wrong} sai · ${part.skipped} bỏ qua',
                        style: const TextStyle(
                          fontSize: 12,
                          color: Mochi.muted,
                        ),
                      ),
                      const SizedBox(height: 10),
                      LinearProgressIndicator(
                        value: part.correct / part.total,
                        color: part.part == 5
                            ? Mochi.primary
                            : part.part == 6
                            ? Mochi.correct
                            : const Color(0xff8c6e61),
                        backgroundColor: Colors.white,
                      ),
                    ],
                  ),
                ),
              ),
            const SizedBox(height: 8),
          ],
          const MochiCard(
            color: Mochi.lavender,
            child: Text(
              'Tỷ lệ đúng được tính trên toàn bộ 100 câu. Câu bỏ trống được tính riêng với câu trả lời sai.',
            ),
          ),
          const SizedBox(height: 24),
          OutlinedButton.icon(
            onPressed: () => Navigator.push(
              context,
              MaterialPageRoute(
                builder: (_) => ReviewScreen(api: api, attemptId: result.id),
              ),
            ),
            icon: const Icon(Icons.lightbulb_outline),
            label: const Text('Xem lại đáp án & giải thích'),
          ),
          const SizedBox(height: 12),
          FilledButton.icon(
            onPressed: () => Navigator.pop(context),
            icon: const Icon(Icons.format_list_bulleted),
            label: const Text('Về danh sách đề thi'),
          ),
        ],
      ),
    ),
  );
}

class _Metric extends StatelessWidget {
  const _Metric(this.value, this.label, this.foreground, this.background);
  final String value, label;
  final Color foreground, background;
  @override
  Widget build(BuildContext context) => Expanded(
    child: Container(
      padding: const EdgeInsets.symmetric(vertical: 16, horizontal: 6),
      decoration: BoxDecoration(
        color: background,
        borderRadius: BorderRadius.circular(12),
      ),
      child: Column(
        children: [
          Text(
            value,
            style: TextStyle(
              fontSize: 25,
              fontWeight: FontWeight.w600,
              color: foreground,
            ),
          ),
          Text(
            label,
            textAlign: TextAlign.center,
            style: const TextStyle(fontSize: 11),
          ),
        ],
      ),
    ),
  );
}
