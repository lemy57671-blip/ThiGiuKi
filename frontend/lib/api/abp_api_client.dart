import 'dart:convert';

import 'package:http/http.dart' as http;

import '../config/app_config.dart';

class ApiException implements Exception {
  const ApiException(this.message);
  final String message;

  @override
  String toString() => message;
}

class LoginInformation {
  const LoginInformation({required this.version});
  final String version;

  factory LoginInformation.fromJson(Map<String, dynamic> json) {
    final application = json['application'];
    if (application is! Map<String, dynamic> ||
        application['version'] is! String) {
      throw const ApiException('Phản hồi phiên bản ứng dụng không hợp lệ.');
    }
    return LoginInformation(version: application['version'] as String);
  }
}

/// Reads the existing ASP.NET Boilerplate AjaxResponse envelope.
class AbpApiClient {
  AbpApiClient({http.Client? client, String baseUrl = AppConfig.apiBaseUrl})
    : _client = client ?? http.Client(),
      _baseUri = Uri.parse(baseUrl.endsWith('/') ? baseUrl : '$baseUrl/') {
    if (!['http', 'https'].contains(_baseUri.scheme) || _baseUri.host.isEmpty) {
      throw ArgumentError.value(baseUrl, 'baseUrl', 'Cần URL HTTP hoặc HTTPS.');
    }
  }

  final http.Client _client;
  final Uri _baseUri;

  Future<LoginInformation> getLoginInformation() async {
    final response = await _client
        .get(
          _baseUri.resolve(
            'api/services/app/Session/GetCurrentLoginInformations',
          ),
          headers: {'Accept': 'application/json'},
        )
        .timeout(const Duration(seconds: 15));

    Object? decoded;
    try {
      decoded = jsonDecode(utf8.decode(response.bodyBytes));
    } on FormatException {
      throw const ApiException('Máy chủ không trả về JSON hợp lệ.');
    }
    if (decoded is! Map<String, dynamic>) {
      throw const ApiException('Phản hồi của máy chủ không hợp lệ.');
    }
    if (response.statusCode < 200 ||
        response.statusCode >= 300 ||
        decoded['success'] != true) {
      final error = decoded['error'];
      throw ApiException(
        error is Map<String, dynamic> && error['message'] is String
            ? error['message'] as String
            : 'Không thể kết nối hệ thống (HTTP ${response.statusCode}).',
      );
    }
    final result = decoded['result'];
    if (result is! Map<String, dynamic>) {
      throw const ApiException('Phản hồi thiếu dữ liệu result.');
    }
    return LoginInformation.fromJson(result);
  }

  void close() => _client.close();
}
