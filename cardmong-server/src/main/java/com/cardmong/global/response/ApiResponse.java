package com.cardmong.global.response;

import com.fasterxml.jackson.annotation.JsonInclude;
import lombok.AccessLevel;
import lombok.AllArgsConstructor;
import lombok.Getter;

@Getter
@AllArgsConstructor(access = AccessLevel.PRIVATE)
@JsonInclude(JsonInclude.Include.NON_NULL)
public class ApiResponse<T> {

    private final boolean success;
    private final T data;
    private final ErrorInfo error;

    public static <T> ApiResponse<T> ok(T data) {
        return new ApiResponse<>(true, data, null);
    }

    public static ApiResponse<?> fail(ErrorCode code) {
        return new ApiResponse<>(false, null, new ErrorInfo(code.name(), code.getMessage()));
    }

    public static ApiResponse<?> failRaw(String code, String message) {
        return new ApiResponse<>(false, null, new ErrorInfo(code, message));
    }

    public record ErrorInfo(String code, String message) {}
}
