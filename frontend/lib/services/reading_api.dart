import 'dart:async';
import 'dart:convert';
import 'package:http/http.dart' as http;
import '../config/app_config.dart';
import '../models/reading_models.dart';

class ReadingApiError implements Exception {
  const ReadingApiError(this.message, {this.status});
  final String message;
  final int? status;
  @override
  String toString() => message;
}

class ReadingApi {
  ReadingApi({http.Client? client, String baseUrl = AppConfig.apiBaseUrl})
    : _client = client ?? http.Client(),
      _base = Uri.parse(baseUrl.endsWith('/') ? baseUrl : '$baseUrl/');
  final http.Client _client;
  final Uri _base;
  String? token;
  int? tenantId = 1;
  Future<void> Function()? onSessionExpired;
  Future<void>? _renewing;
  Future<dynamic> request(String method, String path, [Json? body]) =>
      _request(method, path, body, true);

  Future<dynamic> _request(
    String method,
    String path,
    Json? body,
    bool renew,
  ) async {
    try {
      final headers = <String, String>{
        'Accept': 'application/json',
        'Content-Type': 'application/json',
        if (token != null) 'Authorization': 'Bearer $token',
        if (tenantId != null) 'Abp-TenantId': '$tenantId',
      };
      final response =
          await (method == 'GET'
                  ? _client.get(_base.resolve(path), headers: headers)
                  : _client.post(
                      _base.resolve(path),
                      headers: headers,
                      body: jsonEncode(body ?? {}),
                    ))
              .timeout(const Duration(seconds: 60));
      if (response.statusCode == 401 && renew && onSessionExpired != null) {
        _renewing ??= onSessionExpired!().whenComplete(() => _renewing = null);
        await _renewing;
        return _request(method, path, body, false);
      }
      Json json;
      try {
        final decoded = jsonDecode(utf8.decode(response.bodyBytes));
        if (decoded is! Json) throw const FormatException();
        json = decoded;
      } on FormatException {
        throw ReadingApiError(
          'Máy chủ không trả JSON hợp lệ (HTTP ${response.statusCode}).',
          status: response.statusCode,
        );
      }
      if (response.statusCode < 200 ||
          response.statusCode >= 300 ||
          json['success'] != true) {
        final error = json['error'];
        throw ReadingApiError(
          error is Json && error['message'] is String
              ? error['message'] as String
              : response.statusCode == 401
              ? 'Không thể khôi phục phiên làm bài. Hãy thử lại.'
              : 'Không thể thực hiện yêu cầu (HTTP ${response.statusCode}).',
          status: response.statusCode,
        );
      }
      return json['result'];
    } on TimeoutException {
      throw const ReadingApiError(
        'Kết nối quá thời gian chờ. Kiểm tra mạng và thử lại.',
      );
    } on http.ClientException {
      throw const ReadingApiError(
        'Không thể kết nối máy chủ. Kiểm tra mạng và thử lại.',
      );
    }
  }

  Future<Json> openSession(String? visitorKey) async {
    tenantId = 1;
    token = null;
    final result = await _request('POST', 'api/ReadingAccess/Student', {
      'visitorKey': visitorKey,
    }, false);
    if (result is! Json ||
        result['accessToken'] is! String ||
        result['visitorKey'] is! String ||
        result['expireInSeconds'] is! int ||
        (result['expireInSeconds'] as int) <= 0 ||
        result['tenantId'] != 1) {
      throw const ReadingApiError('Không thể mở phiên làm bài. Hãy thử lại.');
    }
    token = result['accessToken'] as String;
    return result;
  }

  Future<List<ExamInfo>> exams() async =>
      ((await request('GET', 'api/Reading/Exams')) as List)
          .map((j) => ExamInfo.fromJson(j as Json))
          .toList();
  Future<List<AttemptInfo>> attempts() async =>
      ((await request('GET', 'api/Reading/Attempts')) as List)
          .map((j) => AttemptInfo.fromJson(j as Json))
          .toList();
  Future<AttemptInfo> start(int examId, String requestId) async =>
      AttemptInfo.fromJson(
        await request('POST', 'api/Reading/Attempts', {
              'examId': examId,
              'clientRequestId': requestId,
            })
            as Json,
      );
  Future<AttemptInfo> state(String id) async => AttemptInfo.fromJson(
    await request('GET', 'api/Reading/Attempts/$id') as Json,
  );
  Future<PaperInfo> paper(String id) async => PaperInfo.fromJson(
    await request('GET', 'api/Reading/Attempts/$id/Paper') as Json,
  );
  Future<ReviewInfo> review(String id) async => ReviewInfo.fromJson(
    await request('GET', 'api/Reading/Attempts/$id/Review') as Json,
  );
  Future<AnswerInfo> save(
    String id,
    int questionId,
    int? optionId,
    String rowVersion,
  ) async => AnswerInfo.fromJson(
    await request('POST', 'api/Reading/Answer', {
          'attemptId': id,
          'questionId': questionId,
          'selectedOptionId': optionId,
          'expectedRowVersion': rowVersion,
        })
        as Json,
  );
  Future<AttemptInfo> submit(String id) async => AttemptInfo.fromJson(
    await request('POST', 'api/Reading/Attempts/$id/Submit') as Json,
  );
  void close() => _client.close();
}
